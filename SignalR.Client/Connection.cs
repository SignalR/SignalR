using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SignalR.Client.Http;
using SignalR.Client.Transports;

namespace SignalR.Client
{
    /// <summary>
    /// Provides client connections for SignalR services.
    /// </summary>
    public class Connection : IConnection
    {
        private static Version _assemblyVersion;

        private IClientTransport _transport;
        private ConnectionState _state;

        /// <summary>
        /// Occurs when the <see cref="Connection"/> has received data from the server.
        /// </summary>
        public event Action<string> Received;

        /// <summary>
        /// Occurs when the <see cref="Connection"/> has encountered an error.
        /// </summary>
        public event Action<Exception> Error;

        /// <summary>
        /// Occurs when the <see cref="Connection"/> is stopped.
        /// </summary>
        public event Action Closed;

        /// <summary>
        /// Occurs when the <see cref="Connection"/> successfully reconnects after a timeout.
        /// </summary>
        public event Action Reconnected;

        /// <summary>
        /// Occurs when the <see cref="Connection"/> state changes.
        /// </summary>
        public event Action<ConnectionState> StateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        public Connection(string url)
            : this(url, (string)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        public Connection(string url, IDictionary<string, string> queryString)
            : this(url, CreateQueryString(queryString))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="url">The url to connect to.</param>
        /// <param name="queryString">The query string data to pass to the server.</param>
        public Connection(string url, string queryString)
        {
            if (url.Contains("?"))
            {
                throw new ArgumentException("Url cannot contain QueryString directly. Pass QueryString values in using available overload.", "url");
            }

            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            Url = url;
            QueryString = queryString;
            Groups = Enumerable.Empty<string>();
            Items = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            State = ConnectionState.Disconnected;
        }

        /// <summary>
        /// Gets or sets the cookies associated with the connection.
        /// </summary>
        public CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// Gets or sets authentication information for the connection.
        /// </summary>
        public ICredentials Credentials { get; set; }

        /// <summary>
        /// Gets or sets the groups for the connection.
        /// </summary>
        public IEnumerable<string> Groups { get; set; }

        public Func<string> Sending { get; set; }

        /// <summary>
        /// Gets the url for the connection.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Gets or sets the last message id for the connection.
        /// </summary>
        public long? MessageId { get; set; }

        /// <summary>
        /// Gets or sets the connection id for the connection.
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets a dictionary for storing state for a the connection.
        /// </summary>
        public IDictionary<string, object> Items { get; private set; }

        /// <summary>
        /// Gets the querystring specified in the ctor.
        /// </summary>
        public string QueryString { get; private set; }
        
        /// <summary>
        /// Gets the current <see cref="ConnectionState"/> of the connection.
        /// </summary>
        public ConnectionState State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state != value)
                {
                    if (StateChanged != null)
                    {
                        StateChanged(value);
                    }

                    _state = value;
                }
            }
        }

        ConnectionState IConnection.State
        {
            get
            {
                return State;
            }
            set
            {
                State = value;
            }
        }

        /// <summary>
        /// Starts the <see cref="Connection"/>.
        /// </summary>
        /// <returns>A task that represents when the connection has started.</returns>
        public Task Start()
        {
            return Start(new DefaultHttpClient());
        }

        /// <summary>
        /// Starts the <see cref="Connection"/>.
        /// </summary>
        /// <param name="httpClient">The http client</param>
        /// <returns>A task that represents when the connection has started.</returns>
        public Task Start(IHttpClient httpClient)
        {
#if WINDOWS_PHONE || SILVERLIGHT || NETFX_CORE
            return Start(new LongPollingTransport(httpClient));
#else
            // Pick the best transport supported by the client
            return Start(new AutoTransport(httpClient));
#endif
        }

        /// <summary>
        /// Starts the <see cref="Connection"/>.
        /// </summary>
        /// <param name="transport">The transport to use.</param>
        /// <returns>A task that represents when the connection has started.</returns>
        public virtual Task Start(IClientTransport transport)
        {
            if (State == ConnectionState.Connected ||
                State == ConnectionState.Connecting)
            {
                return TaskAsyncHelper.Empty;
            }

            State = ConnectionState.Connecting;

            _transport = transport;

            return Negotiate(transport);
        }

        private Task Negotiate(IClientTransport transport)
        {
            var negotiateTcs = new TaskCompletionSource<object>();

            transport.Negotiate(this).Then(negotiationResponse =>
            {
                VerifyProtocolVersion(negotiationResponse.ProtocolVersion);

                ConnectionId = negotiationResponse.ConnectionId;

                if (Sending != null)
                {
                    var data = Sending();
                    StartTransport(data).ContinueWith(negotiateTcs);
                }
                else
                {
                    StartTransport(null).ContinueWith(negotiateTcs);
                }
            })
            .ContinueWithNotComplete(negotiateTcs);

            var tcs = new TaskCompletionSource<object>();
            negotiateTcs.Task.ContinueWith(task =>
            {
                // If there's any errors starting then Stop the connection                
                if (task.IsFaulted)
                {
                    Stop();
                    tcs.SetException(task.Exception);
                }
                else if (task.IsCanceled)
                {
                    Stop();
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(null);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        private Task StartTransport(string data)
        {
            return _transport.Start(this, data)
                             .Then(() => State = ConnectionState.Connected);
        }

        private static void VerifyProtocolVersion(string versionString)
        {
            Version version;
            if (String.IsNullOrEmpty(versionString) ||
                !TryParseVersion(versionString, out version) ||
                !(version.Major == 1 && version.Minor == 0))
            {
                throw new InvalidOperationException("Incompatible protocol version.");
            }
        }

        /// <summary>
        /// Stops the <see cref="Connection"/>.
        /// </summary>
        public virtual void Stop()
        {
            try
            {
                // Do nothing if the connection is offline
                if (this.IsDisconnecting())
                {
                    return;
                }

                State = ConnectionState.Disconnecting;

                _transport.Stop(this);

                if (Closed != null)
                {
                    Closed();
                }
            }
            finally
            {
                State = ConnectionState.Disconnected;
            }
        }

        /// <summary>
        /// Sends data asynchronously over the connection.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <returns>A task that represents when the data has been sent.</returns>
        public Task Send(string data)
        {
            return ((IConnection)this).Send<object>(data);
        }

        Task<T> IConnection.Send<T>(string data)
        {
            if (this.IsDisconnecting())
            {
                // TODO: Update this error message
                throw new InvalidOperationException("Start must be called before data can be sent");
            }

            return _transport.Send<T>(this, data);
        }

        void IConnection.OnReceived(JToken message)
        {
            OnReceived(message);
        }

        protected virtual void OnReceived(JToken message)
        {
            if (Received != null)
            {
                Received(message.ToString());
            }
        }

        void IConnection.OnError(Exception error)
        {
            if (Error != null)
            {
                Error(error);
            }
        }

        void IConnection.OnReconnected()
        {
            if (Reconnected != null)
            {
                Reconnected();
            }
        }

        void IConnection.PrepareRequest(IRequest request)
        {
#if WINDOWS_PHONE
            // http://msdn.microsoft.com/en-us/library/ff637320(VS.95).aspx
            request.UserAgent = CreateUserAgentString("SignalR.Client.WP7");
#else
#if SILVERLIGHT
            // Useragent is not possible to set with Silverlight, not on the UserAgent property of the request nor in the Headers key/value in the request
#else
            request.UserAgent = CreateUserAgentString("SignalR.Client");
#endif
#endif
            if (Credentials != null)
            {
                request.Credentials = Credentials;
            }

            if (CookieContainer != null)
            {
                request.CookieContainer = CookieContainer;
            }
        }

        private static string CreateUserAgentString(string client)
        {
            if (_assemblyVersion == null)
            {
#if NETFX_CORE
                _assemblyVersion = new Version("0.5.0.0");
#else
                _assemblyVersion = new AssemblyName(typeof(Connection).Assembly.FullName).Version;
#endif
            }

#if NETFX_CORE
            return String.Format(CultureInfo.InvariantCulture, "{0}/{1} ({2})", client, _assemblyVersion, "Unknown OS");
#else
            return String.Format(CultureInfo.InvariantCulture, "{0}/{1} ({2})", client, _assemblyVersion, Environment.OSVersion);
#endif
        }

        private static bool TryParseVersion(string versionString, out Version version)
        {
#if WINDOWS_PHONE
            try
            {
                version = new Version(versionString);
                return true;
            }
            catch
            {
                version = null;
                return false;
            }
#else
            return Version.TryParse(versionString, out version);
#endif
        }

        private static string CreateQueryString(IDictionary<string, string> queryString)
        {
            return String.Join("&", queryString.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray());
        }
    }
}
