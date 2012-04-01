using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using SignalR.Client.Http;
using SignalR.Client.Transports;

namespace SignalR.Client
{
    public class Connection : IConnection
    {
        private static Version _assemblyVersion;

        private IClientTransport _transport;
        private bool _initialized;

        public event Action<string> Received;
        public event Action<Exception> Error;
        public event Action Closed;
        public event Action Reconnected;

        public Connection(string url)
            : this(url, (string)null)
        {

        }

        public Connection(string url, IDictionary<string, string> queryString)
            : this(url, CreateQueryString(queryString))
        {

        }

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
        }

        public CookieContainer CookieContainer { get; set; }

        public ICredentials Credentials { get; set; }

        public IEnumerable<string> Groups { get; set; }

        public Func<string> Sending { get; set; }

        public string Url { get; private set; }

        public bool IsActive { get; private set; }

        public long? MessageId { get; set; }

        public string ConnectionId { get; set; }

        public IDictionary<string, object> Items { get; private set; }

        public string QueryString { get; private set; }

        public Task Start()
        {
            return Start(new DefaultHttpClient());
        }

        public Task Start(IHttpClient httpClient)
        {
#if WINDOWS_PHONE || SILVERLIGHT
            return Start(new LongPollingTransport());
#else
            // Pick the best transport supported by the client
            return Start(new AutoTransport(httpClient));
#endif
        }

        public virtual Task Start(IClientTransport transport)
        {
            if (IsActive)
            {
                return TaskAsyncHelper.Empty;
            }

            IsActive = true;

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

            return negotiateTcs.Task;
        }

        private Task StartTransport(string data)
        {
            return _transport.Start(this, data)
                             .Then(() => _initialized = true);
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

        public virtual void Stop()
        {
            try
            {
                // Do nothing if the connection was never started
                if (!_initialized)
                {
                    return;
                }

                _transport.Stop(this);

                if (Closed != null)
                {
                    Closed();
                }
            }
            finally
            {
                IsActive = false;
                _initialized = false;
            }
        }

        public Task Send(string data)
        {
            return Send<object>(data);
        }

        public Task<T> Send<T>(string data)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Start must be called before data can be sent");
            }

            return _transport.Send<T>(this, data);
        }

        void IConnection.OnReceived(string message)
        {
            if (Received != null)
            {
                Received(message);
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
