// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if (NET4 || NET45)
using System.Security.Cryptography.X509Certificates;
#endif

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    /// Provides client connections for SignalR services.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "_disconnectCts is disposed on disconnect.")]
    public class Connection : IConnection, IDisposable
    {
        internal static readonly TimeSpan DefaultAbortTimeout = TimeSpan.FromSeconds(30);

        private static Version _assemblyVersion;

        private IClientTransport _transport;

        // Propagates notification that connection should be stopped.
        private CancellationTokenSource _disconnectCts;

        // The amount of time the client should attempt to reconnect before stopping.
        private TimeSpan _disconnectTimeout;

        // The amount of time a transport will wait (while connecting) before failing. 
        private TimeSpan _totalTransportConnectTimeout;

        // Provides a way to cancel the the timeout that stops a reconnect cycle
        private IDisposable _disconnectTimeoutOperation;

        // The default connection state is disconnected
        private ConnectionState _state;

        private KeepAliveData _keepAliveData;

        private TimeSpan _reconnectWindow;

        private Task _connectTask;

        private TextWriter _traceWriter;

        private string _connectionData;

        private TaskQueue _receiveQueue;

        // Used to monitor for possible deadlocks in the _receiveQueue
        // and trigger OnError if any are detected.
        private TaskQueueMonitor _receiveQueueMonitor;

        private Task _lastQueuedReceiveTask;

        private TaskCompletionSource<object> _startTcs;

        // Used to synchronize state changes
        private readonly object _stateLock = new object();

        // Used to synchronize starting and stopping specifically
        private readonly object _startLock = new object();

        // Used to ensure we don't write to the Trace TextWriter from multiple threads simultaneously 
        private readonly object _traceLock = new object();

        private DateTime _lastMessageAt;

        // Indicates the last time we marked the C# code as running.
        private DateTime _lastActiveAt;

        //The json serializer for the connections
        private JsonSerializer _jsonSerializer = new JsonSerializer();

#if (NET4 || NET45)
        private readonly X509CertificateCollection _certCollection = new X509CertificateCollection();
#endif

        // Keeps track of when the last keep alive from the server was received
        // internal virtual to allow mocking
        internal virtual HeartbeatMonitor Monitor { get; private set; }

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
        /// Occurs when the <see cref="Connection"/> is about to timeout
        /// </summary>
        public event Action ConnectionSlow;

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
            _lastMessageAt = DateTime.UtcNow;
            _lastActiveAt = DateTime.UtcNow;
            _reconnectWindow = TimeSpan.Zero;
            Items = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            State = ConnectionState.Disconnected;
            TraceLevel = TraceLevels.All;
            TraceWriter = new DebugTextWriter();
            Headers = new HeaderDictionary(this);
            TransportConnectTimeout = TimeSpan.Zero;
            _totalTransportConnectTimeout = TimeSpan.Zero;
            DeadlockErrorTimeout = TimeSpan.FromSeconds(10);

            // Current client protocol
            Protocol = new Version(1, 4);
        }

        /// <summary>
        /// The amount of time a transport will wait (while connecting) before failing.
        /// This value is modified by adding the server's TransportConnectTimeout configuration value.
        /// </summary>
        public TimeSpan TransportConnectTimeout { get; set; }

        /// <summary>
        /// Gets or sets the amount of time a callback registered with "HubProxy.On" or
        /// "Connection.Received" may run before <see cref="Connection.Error"/> will be called
        /// warning that a possible deadlock has been detected.
        /// </summary>
        public TimeSpan DeadlockErrorTimeout { get; set; }

        /// <summary>
        /// The amount of time a transport will wait (while connecting) before failing.
        /// This is the total vaue obtained by adding the server's configuration value and the timeout specified by the user
        /// </summary>
        TimeSpan IConnection.TotalTransportConnectTimeout
        {
            get
            {
                return _totalTransportConnectTimeout;
            }
        }

        public Version Protocol { get; set; }

        /// <summary>
        /// Gets the last error encountered by the <see cref="Connection"/>.
        /// </summary>
        public Exception LastError { get; private set; }

        /// <summary>
        /// The maximum amount of time a connection will allow to try and reconnect.
        /// This value is equivalent to the summation of the servers disconnect and keep alive timeout values.
        /// </summary>
        TimeSpan IConnection.ReconnectWindow
        {
            get
            {
                return _reconnectWindow;
            }
            set
            {
                _reconnectWindow = value;
            }
        }

        /// <summary>
        /// Object to store the various keep alive timeout values
        /// </summary>
        KeepAliveData IConnection.KeepAliveData
        {
            get
            {
                return _keepAliveData;
            }
            set
            {
                _keepAliveData = value;
            }
        }

        /// <summary>
        /// The timestamp of the last message received by the connection.
        /// </summary>
        DateTime IConnection.LastMessageAt
        {
            get
            {
                return _lastMessageAt;
            }
        }

        DateTime IConnection.LastActiveAt
        {
            get
            {
                return _lastActiveAt;
            }
        }

#if NET4 || NET45
        X509CertificateCollection IConnection.Certificates
        {
            get
            {
                return _certCollection;
            }
        }
#endif

        public TraceLevels TraceLevel { get; set; }

        public TextWriter TraceWriter
        {
            get
            {
                return _traceWriter;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _traceWriter = value;
            }
        }

        /// <summary>
        /// Gets or sets the serializer used by the connection
        /// </summary>
        public JsonSerializer JsonSerializer
        {
            get
            {
                return _jsonSerializer;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _jsonSerializer = value;
            }
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
        /// Gets and sets headers for the requests
        /// </summary>
        public IDictionary<string, string> Headers { get; private set; }

#if !PORTABLE
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
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This is disposed on close")]
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
            if (transport == null)
            {
                throw new ArgumentNullException("transport");
            }

            lock (_startLock)
            {
                if (!ChangeState(ConnectionState.Disconnected, ConnectionState.Connecting))
                {
                    return _connectTask ?? TaskAsyncHelper.Empty;
                }

                _disconnectCts = new CancellationTokenSource();
                _startTcs = new TaskCompletionSource<object>();
                _receiveQueueMonitor = new TaskQueueMonitor(this, DeadlockErrorTimeout);
                _receiveQueue = new TaskQueue(_startTcs.Task, _receiveQueueMonitor);
                _lastQueuedReceiveTask = TaskAsyncHelper.Empty;

                _transport = transport;

                _connectTask = Negotiate(transport);
            }

            return _connectTask;
        }

        protected virtual string OnSending()
        {
            return null;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is flowed back to the caller via the tcs.")]
        private Task Negotiate(IClientTransport transport)
        {
            _connectionData = OnSending();

            return transport.Negotiate(this, _connectionData)
                            .Then(negotiationResponse =>
                            {
                                VerifyProtocolVersion(negotiationResponse.ProtocolVersion);

                                ConnectionId = negotiationResponse.ConnectionId;
                                ConnectionToken = negotiationResponse.ConnectionToken;
                                _disconnectTimeout = TimeSpan.FromSeconds(negotiationResponse.DisconnectTimeout);
                                _totalTransportConnectTimeout = TransportConnectTimeout + TimeSpan.FromSeconds(negotiationResponse.TransportConnectTimeout);

                                // Default the beat interval to be 5 seconds in case keep alive is disabled.
                                var beatInterval = TimeSpan.FromSeconds(5);

                                // If we have a keep alive
                                if (negotiationResponse.KeepAliveTimeout != null)
                                {
                                    _keepAliveData = new KeepAliveData(TimeSpan.FromSeconds(negotiationResponse.KeepAliveTimeout.Value));
                                    _reconnectWindow = _disconnectTimeout + _keepAliveData.Timeout;

                                    beatInterval = _keepAliveData.CheckInterval;
                                }
                                else
                                {
                                    _reconnectWindow = _disconnectTimeout;
                                }

                                Monitor = new HeartbeatMonitor(this, _stateLock, beatInterval);

                                return StartTransport();
                            })
                            .ContinueWithNotComplete(() => Disconnect());
        }

        private Task StartTransport()
        {
            return _transport.Start(this, _connectionData, _disconnectCts.Token)
                             .RunSynchronously(() =>
                             {
                                 // NOTE: We have tests that rely on this state change occuring *BEFORE* the start task is complete
                                 ChangeState(ConnectionState.Connecting, ConnectionState.Connected);

                                 // Now that we're connected complete the start task that the
                                 // receive queue is waiting on
                                 _startTcs.SetResult(null);

                                 // Start the monitor to check for server activity
                                 _lastMessageAt = DateTime.UtcNow;
                                 _lastActiveAt = DateTime.UtcNow;
                                 Monitor.Start();
                             })
                             // Don't return until the last receive has been processed to ensure messages/state sent in OnConnected
                             // are processed prior to the Start() method task finishing
                             .Then(() => _lastQueuedReceiveTask);
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
                    Trace(TraceLevels.StateChanges, "ChangeState({0}, {1})", oldState, newState);

                    State = newState;
                    return true;
                }
            }

            // Invalid transition
            return false;
        }

        private void VerifyProtocolVersion(string versionString)
        {
            Version version;

            if (String.IsNullOrEmpty(versionString) ||
                !TryParseVersion(versionString, out version) ||
                version != Protocol)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                                                                  Resources.Error_IncompatibleProtocolVersion,
                                                                  Protocol,
                                                                  versionString ?? "null"));
            }
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> and sends an abort message to the server.
        /// </summary>
        public void Stop()
        {
            Stop(DefaultAbortTimeout);
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> and sends an abort message to the server.
        /// <param name="error">The error due to which the connection is being stopped.</param>
        /// </summary>
        public void Stop(Exception error)
        {
            Stop(error, DefaultAbortTimeout);
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> and sends an abort message to the server.
        /// <param name="error">The error due to which the connection is being stopped.</param>
        /// <param name="timeout">The timeout</param>
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want to raise the Start exception on Stop.")]
        public void Stop(Exception error, TimeSpan timeout)
        {
            OnError(error);
            Stop(timeout);            
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> and sends an abort message to the server.
        /// <param name="timeout">The timeout</param>
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want to raise the Start exception on Stop.")]
        public void Stop(TimeSpan timeout)
        {
            lock (_startLock)
            {
                // Wait for the connection to connect
                if (_connectTask != null)
                {
                    try
                    {
                        _connectTask.Wait(timeout);
                    }
                    catch (Exception ex)
                    {
                        Trace(TraceLevels.Events, "Error: {0}", ex.GetBaseException());
                    }
                }

                if (_receiveQueue != null)
                {
                    // Close the receive queue so currently running receive callback finishes and no more are run.
                    // We can't wait on the result of the drain because this method may be on the stack of the task returned (aka deadlock).
                    _receiveQueue.Drain().Catch(this);
                }

                // This is racy since it's outside the _stateLock, but we are trying to avoid 30s deadlocks when calling _transport.Abort()
                if (State == ConnectionState.Disconnected)
                {
                    return;
                }

                Trace(TraceLevels.Events, "Stop");

                // Dispose the heart beat monitor so we don't fire notifications when waiting to abort
                Monitor.Dispose();

                _transport.Abort(this, timeout, _connectionData);

                Disconnect();
            }
        }

        /// <summary>
        /// Stops the <see cref="Connection"/> without sending an abort message to the server.
        /// This function is called after we receive a disconnect message from the server.
        /// </summary>
        void IConnection.Disconnect()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            lock (_stateLock)
            {
                // Do nothing if the connection is offline
                if (State != ConnectionState.Disconnected)
                {
                    // Change state before doing anything else in case something later in the method throws
                    State = ConnectionState.Disconnected;

                    Trace(TraceLevels.StateChanges, "Disconnected");

                    _disconnectTimeoutOperation.Dispose();
                    _disconnectCts.Cancel();
                    _disconnectCts.Dispose();

                    _receiveQueueMonitor.Dispose();

                    if (Monitor != null)
                    {
                        Monitor.Dispose();
                    }

                    if (_transport != null)
                    {
                        Trace(TraceLevels.Events, "Transport.Dispose({0})", ConnectionId);

                        _transport.Dispose();
                        _transport = null;
                    }

                    Trace(TraceLevels.Events, "Closed");

                    // Clear the state for this connection
                    ConnectionId = null;
                    ConnectionToken = null;
                    GroupsToken = null;
                    MessageId = null;
                    _connectionData = null;

#if NETFX_CORE || PORTABLE
                    // Clear the buffer
                    _traceWriter.Flush();
#endif
                    // TODO: Do we want to trigger Closed if we are connecting?
                    OnClosed();
                }
            }
        }

        protected virtual void OnClosed()
        {
            if (Closed != null)
            {
                Closed();
            }
        }

        /// <summary>
        /// Sends data asynchronously over the connection.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <returns>A task that represents when the data has been sent.</returns>
        public virtual Task Send(string data)
        {
            if (State == ConnectionState.Disconnected)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_DataCannotBeSentConnectionDisconnected));
            }

            if (State == ConnectionState.Connecting)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ConnectionHasNotBeenEstablished));
            }

            return _transport.Send(this, data, _connectionData);
        }

        /// <summary>
        /// Sends an object that will be JSON serialized asynchronously over the connection.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>A task that represents when the data has been sent.</returns>
        public Task Send(object value)
        {
            return Send(this.JsonSerializeObject(value));
        }

#if (NET4 || NET45)
        /// <summary>
        /// Adds a client certificate to the request
        /// </summary>
        /// <param name="certificate">Client Certificate</param>
        public void AddClientCertificate(X509Certificate certificate)
        {
            lock (_stateLock)
            {
                if (State != ConnectionState.Disconnected)
                {
                    throw new InvalidOperationException(Resources.Error_CertsCanOnlyBeAddedWhenDisconnected);
                }

                _certCollection.Add(certificate);
            }
        }
#endif

        public void Trace(TraceLevels level, string format, params object[] args)
        {
            lock (_traceLock)
            {
                if (TraceLevel.HasFlag(level))
                {
                    _traceWriter.WriteLine(
                        DateTime.UtcNow.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + " - " +
                            (ConnectionId ?? "null") + " - " +
                            format,
                        args);
                }
            }
        }


        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by the transport layer")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception is raised via an event.")]
        void IConnection.OnReceived(JToken message)
        {
            _lastQueuedReceiveTask = _receiveQueue.Enqueue(() => Task.Factory.StartNew(() =>
            {
                try
                {
                    OnMessageReceived(message);
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }));
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The exception can be from user code, needs to be a catch all.")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by the transport layer")]
        protected virtual void OnMessageReceived(JToken message)
        {
            if (Received != null)
            {
                // #1889: We now have a try-catch in the OnMessageReceived handler.  One note about this change is that
                // messages that are triggered via responses to server invocations will no longer be wrapped in an
                // aggregate exception due to this change.  This makes the exception throwing behavior consistent across
                // all types of receive triggers.
                try
                {
                    Received(message.ToString());
                }
                catch (Exception ex)
                {
                    OnError(ex);
                }
            }
        }

        private void OnError(Exception error)
        {
            Trace(TraceLevels.Events, "OnError({0})", error);

            LastError = error;

            if (Error != null)
            {
                Error(error);
            }
        }

        void IConnection.OnError(Exception error)
        {
            OnError(error);
        }

        void IConnection.OnReconnecting()
        {
            OnReconnecting();
        }

        internal virtual void OnReconnecting()
        {
            // Only allow the client to attempt to reconnect for a _disconnectTimout TimeSpan which is set by
            // the server during negotiation.
            // If the client tries to reconnect for longer the server will likely have deleted its ConnectionId
            // topic along with the contained disconnect message.
            _disconnectTimeoutOperation = 
                SetTimeout(
                    _disconnectTimeout,
                    () =>
                    {
                        OnError(new TimeoutException(String.Format(CultureInfo.CurrentCulture,
                                Resources.Error_ReconnectTimeout, _disconnectTimeout)));
                        Disconnect();
                    });

#if NETFX_CORE || PORTABLE
            // Clear the buffer
            _traceWriter.Flush();
#endif

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

            Monitor.Reconnected();
            ((IConnection)this).MarkLastMessage();
        }

        void IConnection.OnConnectionSlow()
        {
            Trace(TraceLevels.Events, "OnConnectionSlow");

            if (ConnectionSlow != null)
            {
                ConnectionSlow();
            }
        }

        /// <summary>
        /// Sets LastMessageAt to the current time 
        /// </summary>
        void IConnection.MarkLastMessage()
        {
            _lastMessageAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets LastActiveAt to the current time 
        /// </summary>
        void IConnection.MarkActive()
        {
            // Ensure that we haven't gone to sleep since our last "active" marking.
            if (TransportHelper.VerifyLastActive(this))
            {
                _lastActiveAt = DateTime.UtcNow;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by the transport layer")]
        void IConnection.PrepareRequest(IRequest request)
        {
#if PORTABLE
            // Cannot set user agent for Portable because SL does not support it.
#elif NETFX_CORE
            request.UserAgent = CreateUserAgentString("SignalR.Client.WinRT");
#elif NET45
            request.UserAgent = CreateUserAgentString("SignalR.Client.NET45");
#else
            request.UserAgent = CreateUserAgentString("SignalR.Client.NET4");
#endif
            request.SetRequestHeaders(Headers);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Can be called via other clients.")]
        private static string CreateUserAgentString(string client)
        {
            if (_assemblyVersion == null)
            {
#if NETFX_CORE
                _assemblyVersion = new Version("2.2.0");
#else
                _assemblyVersion = new AssemblyName(typeof(Connection).Assembly.FullName).Version;
#endif
            }

#if NETFX_CORE || PORTABLE
            return String.Format(CultureInfo.InvariantCulture, "{0}/{1} ({2})", client, _assemblyVersion, "Unknown OS");
#else
            return String.Format(CultureInfo.InvariantCulture, "{0}/{1} ({2})", client, _assemblyVersion, Environment.OSVersion);
#endif
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The Version constructor can throw exceptions of many different types. Failure is indicated by returning false.")]
        private static bool TryParseVersion(string versionString, out Version version)
        {
#if PORTABLE
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

        /// <summary>
        /// Default text writer
        /// </summary>
        private class DebugTextWriter : TextWriter
        {
#if NETFX_CORE || PORTABLE
            private readonly StringBuilder _buffer;
#endif

            public DebugTextWriter()
                : base(CultureInfo.InvariantCulture)
            {
#if NETFX_CORE || PORTABLE
                _buffer = new StringBuilder();
#endif
            }

            public override void WriteLine(string value)
            {
                Debug.WriteLine(value);
            }

#if NETFX_CORE || PORTABLE
            public override void Write(char value)
            {
                lock (_buffer)
                {
                    if (value == '\n')
                    {
                        Flush();
                    }
                    else
                    {
                        _buffer.Append(value);
                    }
                }
            }
#endif

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

#if NETFX_CORE || PORTABLE
            public override void Flush()
            {
                Debug.WriteLine(_buffer.ToString());
                _buffer.Clear();
            }
#endif
        }

        /// <summary>
        /// Stop the connection, equivalent to calling connection.stop
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stop the connection, equivalent to calling connection.stop
        /// </summary>
        /// <param name="disposing">Set this to true to perform the dispose, false to do nothing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }
    }
}
