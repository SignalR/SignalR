// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    /// Provides client connections for SignalR services.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification="_disconnectCts is disposed on disconnect.")]
    public class Connection : IConnection
    {
        private static Version _assemblyVersion;

        private IClientTransport _transport;

        // Propagates notification that connection should be stopped.
        private SafeCancellationTokenSource _disconnectCts;

        // The amount of time the client should attempt to reconnect before stopping.
        private TimeSpan _disconnectTimeout;

        // Provides a way to cancel the the timeout that stops a reconnect cycle
        private IDisposable _disconnectTimeoutOperation;

        // The default connection state is disconnected
        private ConnectionState _state;

        // Used to synchronize state changes
        private readonly object _stateLock = new object();

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
        /// Occurs when the <see cref="Connection"/> starts reconnecting after an error.
        /// </summary>
        public event Action Reconnecting;

        /// <summary>
        /// Occurs when the <see cref="Connection"/> successfully reconnects after a timeout.
        /// </summary>
        public event Action Reconnected;

        /// <summary>
        /// Occurs when the <see cref="Connection"/> state changes.
        /// </summary>
        public event Action<StateChange> StateChanged;

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
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            if (url.Contains("?"))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.Error_UrlCantContainQueryStringDirectly), "url");
            }

            if (!url.EndsWith("/", StringComparison.Ordinal))
            {
                url += "/";
            }

            Url = url;
            QueryString = queryString;
            _disconnectTimeoutOperation = DisposableAction.Empty;
            Items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
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

#if !SILVERLIGHT
        /// <summary>
        /// Gets of sets proxy information for the connection.
        /// </summary>
        public IWebProxy Proxy { get; set; }
#endif

        /// <summary>
        /// Gets the url for the connection.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Gets or sets the last message id for the connection.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Gets or sets the connection id for the connection.
        /// </summary>
        public string ConnectionId { get; set; }
        
        /// <summary>
        /// Gets or sets the connection token for the connection.
        /// </summary>
        public string ConnectionToken { get; set; }

        /// <summary>
        /// Gets or sets the groups token for the connection.
        /// </summary>
        public string GroupsToken { get; set; }

        /// <summary>
        /// Gets a dictionary for storing state for a the connection.
        /// </summary>
        public IDictionary<string, object> Items { get; private set; }

        /// <summary>
        /// Gets the querystring specified in the ctor.
        /// </summary>
        public string QueryString { get; private set; }

        public IClientTransport Transport
        {
            get
            {
                return _transport;
            }
        }

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
                lock (_stateLock)
                {
                    if (_state != value)
                    {
                        var stateChange = new StateChange(oldState: _state, newState: value);
                        _state = value;
                        if (StateChanged != null)
                        {
                            StateChanged(stateChange);
                        }
                    }
                }
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
            // Pick the best transport supported by the client
            return Start(new AutoTransport(httpClient));
        }

        /// <summary>
        /// Starts the <see cref="Connection"/>.
        /// </summary>
        /// <param name="transport">The transport to use.</param>
        /// <returns>A task that represents when the connection has started.</returns>
        public Task Start(IClientTransport transport)
        {
            _disconnectCts = new SafeCancellationTokenSource();
            if (!ChangeState(ConnectionState.Disconnected, ConnectionState.Connecting))
            {
                return TaskAsyncHelper.Empty;
            }

            _transport = transport;

            return Negotiate(transport);
        }

        protected virtual string OnSending()
        {
            return null;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is flowed back to the caller via the tcs.")]
        private Task Negotiate(IClientTransport transport)
        {
            var negotiateTcs = new TaskCompletionSource<object>();

            transport.Negotiate(this).Then(negotiationResponse =>
            {
                VerifyProtocolVersion(negotiationResponse.ProtocolVersion);

                ConnectionId = negotiationResponse.ConnectionId;
                ConnectionToken = negotiationResponse.ConnectionToken;

                _disconnectTimeout = TimeSpan.FromSeconds(negotiationResponse.DisconnectTimeout);

                var data = OnSending();
                StartTransport(data).ContinueWith(negotiateTcs);
            })
            .ContinueWithNotComplete(negotiateTcs);

            var tcs = new TaskCompletionSource<object>();
            negotiateTcs.Task.ContinueWith(task =>
            {
                try
                {
                    // If there's any errors starting then Stop the connection                
                    if (task.IsFaulted)
                    {
                        Disconnect();
                        tcs.SetException(task.Exception.Unwrap());
                    }
                    else if (task.IsCanceled)
                    {
                        Disconnect();
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }

        private Task StartTransport(string data)
        {
            return _transport.Start(this, data, _disconnectCts.Token)
                             .Then(() =>
                             {
                                 ChangeState(ConnectionState.Connecting, ConnectionState.Connected);
                             });
        }

        private bool ChangeState(ConnectionState oldState, ConnectionState newState)
        {
            return ((IConnection)this).ChangeState(oldState, newState);
        }

        bool IConnection.ChangeState(ConnectionState oldState, ConnectionState newState)
        {
            lock (_stateLock)
            {
                // If we're in the expected old state then change state and return true
                if (_state == oldState)
                {
                    State = newState;
                    return true;
                }
            }

            // Invalid transition
            return false;
        }

        private static void VerifyProtocolVersion(string versionString)
        {
            Version version;
            if (String.IsNullOrEmpty(versionString) ||
                !TryParseVersion(versionString, out version) ||
                !(version.Major == 1 && version.Minor == 2))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_IncompatibleProtocolVersion));
            }
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> and sends an abort message to the server.
        /// </summary>
        public virtual void Stop()
        {
            lock (_stateLock)
            {
                // Do nothing if the connection is offline
                if (State != ConnectionState.Disconnected)
                {
                    _transport.Abort(this);
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> without sending an abort message to the server.
        /// </summary>
        public void Disconnect()
        {
            lock (_stateLock)
            {
                // Do nothing if the connection is offline
                if (State != ConnectionState.Disconnected)
                {
                    _disconnectTimeoutOperation.Dispose();
                    _disconnectCts.Cancel();
                    _disconnectCts.Dispose();

                    State = ConnectionState.Disconnected;

                    // TODO: Do we want to trigger Closed if we are connecting?
                    if (Closed != null)
                    {
                        Closed();
                    }
                }
            }
        }

        /// <summary>
        /// Sends data asynchronously over the connection.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <returns>A task that represents when the data has been sent.</returns>
        public Task Send(string data)
        {
            if (State == ConnectionState.Disconnected)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_StartMustBeCalledBeforeDataCanBeSent));
            }

            if (State == ConnectionState.Connecting)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ConnectionHasNotBeenEstablished));
            }

            return _transport.Send(this, data);
        }

        /// <summary>
        /// Sends an object that will be JSON serialized asynchronously over the connection.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A task that represents when the data has been sent.</returns>
        public Task Send(object value)
        {
            return Send(JsonConvert.SerializeObject(value));
        }



        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by the transport layer")]
        void IConnection.OnReceived(JToken message)
        {
            OnMessageReceived(message);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by the transport layer")]
        protected virtual void OnMessageReceived(JToken message)
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

        void IConnection.OnReconnecting()
        {
            // Only allow the client to attempt to reconnect for a _disconnectTimout TimeSpan which is set by
            // the server during negotiation.
            // If the client tries to reconnect for longer the server will likely have deleted its ConnectionId
            // topic along with the contained disconnect message.
            _disconnectTimeoutOperation = SetTimeout(_disconnectTimeout, Disconnect);

            if (Reconnecting != null)
            {
                Reconnecting();
            }
        }

        void IConnection.OnReconnected()
        {
            // Prevent the timeout set OnReconnecting from firing and stopping the connection if we have successfully
            // reconnected before the _disconnectTimeout delay.
            _disconnectTimeoutOperation.Dispose();

            if (Reconnected != null)
            {
                Reconnected();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by the transport layer")]
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
#if !SILVERLIGHT
            if (Proxy != null)
            {
                request.Proxy = Proxy;
            }
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Can be called via other clients.")]
        private static string CreateUserAgentString(string client)
        {
            if (_assemblyVersion == null)
            {
#if NETFX_CORE
                _assemblyVersion = new Version("1.0.0");
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The Version constructor can throw exceptions of many different types. Failure is indicated by returning false.")]
        private static bool TryParseVersion(string versionString, out Version version)
        {
#if WINDOWS_PHONE || NET35
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

        // TODO: Refactor into a helper class
        private static IDisposable SetTimeout(TimeSpan delay, Action operation)
        {
            var cancellableInvoker = new ThreadSafeInvoker();

            TaskAsyncHelper.Delay(delay).Then(() => cancellableInvoker.Invoke(operation));

            // Disposing this return value will cancel the operation if it has not already been invoked.
            return new DisposableAction(() => cancellableInvoker.Invoke());
        }
    }
}
