using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SignalR.Infrastructure;
using SignalR.Transports;

namespace SignalR
{
    /// <summary>
    /// Represents a connection between client and server.
    /// </summary>
    public abstract class PersistentConnection
    {
        private const string WebSocketsTransportName = "webSockets";

        protected IMessageBus _messageBus;
        protected INewMessageBus _newMessageBus;
        protected IJsonSerializer _jsonSerializer;
        protected IConnectionIdGenerator _connectionIdGenerator;
        private ITransportManager _transportManager;
        private bool _initialized;

        protected ITraceManager _trace;
        protected ITransport _transport;
        private IServerCommandHandler _serverMessageHandler;

        public virtual void Initialize(IDependencyResolver resolver)
        {
            if (_initialized)
            {
                return;
            }

            _messageBus = resolver.Resolve<IMessageBus>();
            _newMessageBus = resolver.Resolve<INewMessageBus>();
            _connectionIdGenerator = resolver.Resolve<IConnectionIdGenerator>();
            _jsonSerializer = resolver.Resolve<IJsonSerializer>();
            _transportManager = resolver.Resolve<ITransportManager>();
            _trace = resolver.Resolve<ITraceManager>();
            _serverMessageHandler = resolver.Resolve<IServerCommandHandler>();

            _initialized = true;
        }

        protected virtual TraceSource Trace
        {
            get
            {
                return _trace["SignalR.PersistentConnection"];
            }
        }

        /// <summary>
        /// Occurs when a data is received from a connection.
        /// </summary>
        public static event Action Receiving;

        /// <summary>
        /// Occurs when a new connection is established.
        /// </summary>
        public static event Action<string> Connected;

        /// <summary>
        /// Occurs when an existing connection ends.
        /// </summary>
        public static event Action<string> Disconnected;

        /// <summary>
        /// Gets the <see cref="IConnection"/> for the <see cref="PersistentConnection"/>.
        /// </summary>
        public IConnection Connection
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="IGroupManager"/> for the <see cref="PersistentConnection"/>.
        /// </summary>
        public IGroupManager Groups
        {
            get;
            private set;
        }

        private string DefaultSignal
        {
            get
            {
                return GetType().FullName;
            }
        }

        /// <summary>
        /// Handles all requests for <see cref="PersistentConnection"/>s.
        /// </summary>
        /// <param name="context">The <see cref="HostContext"/> for the current request.</param>
        /// <returns>A <see cref="Task"/> that completes when the <see cref="PersistentConnection"/> pipeline is complete.</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// Thrown if connection wasn't initialized.
        /// Thrown if the transport wasn't specified.
        /// Thrown if the connection id wasn't specified.
        /// </exception>
        public virtual Task ProcessRequestAsync(HostContext context)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Connection not initialized.");
            }

            if (IsNegotiationRequest(context.Request))
            {
                return ProcessNegotiationRequest(context);
            }

            _transport = GetTransport(context);

            if (_transport == null)
            {
                throw new InvalidOperationException("Protocol error: Unknown transport.");
            }

            string connectionId = _transport.ConnectionId;

            // If there's no connection id then this is a bad request
            if (String.IsNullOrEmpty(connectionId))
            {
                throw new InvalidOperationException("Protocol error: Missing connection id.");
            }

            var groups = new List<string>(_transport.Groups);

            Connection connection = CreateConnection(connectionId, groups, context.Request);

            Connection = connection;
            Groups = new GroupManager(connection, DefaultSignal);

            _transport.TransportConnected = () =>
            {
                var command = new ServerCommand
                {
                    Type = ServerCommandType.RemoveConnection,
                    Value = connectionId
                };

                return _serverMessageHandler.SendCommand(command);
            };

            _transport.Connected = () =>
            {
                return OnConnectedAsync(context.Request, connectionId);
            };

            _transport.Reconnected = () =>
            {
                return OnReconnectedAsync(context.Request, groups, connectionId);
            };

            _transport.Received = data =>
            {
                return OnReceivedAsync(context.Request, connectionId, data);
            };

            _transport.Error = OnErrorAsync;

            _transport.Disconnected = () =>
            {
                return OnDisconnectAsync(connectionId);
            };

            return _transport.ProcessRequest(connection) ?? TaskAsyncHelper.Empty;
        }

        protected virtual Connection CreateConnection(string connectionId, IEnumerable<string> groups, IRequest request)
        {
            return new Connection(_messageBus,
                                  _newMessageBus,
                                  _jsonSerializer,
                                  DefaultSignal,
                                  connectionId,
                                  GetDefaultSignals(connectionId),
                                  groups,
                                  _trace);
        }

        /// <summary>
        /// Returns the default signals for the <see cref="PersistentConnection"/>.
        /// </summary>
        /// <param name="connectionId">The id of the incoming connection.</param>
        /// <returns>The default signals for this <see cref="PersistentConnection"/>.</returns>
        protected IEnumerable<string> GetDefaultSignals(string connectionId)
        {
            // The list of default signals this connection cares about:
            // 1. The default signal (the type name)
            // 2. The connection id (so we can message this particular connection)
            // 3. Scoped Connection id + SIGNALRCOMMAND -> for built in commands that we need to process (for this connection only)
            // 4. Connection id + SIGNALRCOMMAND -> for built in commands that we need to process

            // Create a scoped connection id
            string scopedConnectionId = DefaultSignal + "." + connectionId;

            return new string[] {
                DefaultSignal,
                connectionId,
                SignalCommand.AddCommandSuffix(scopedConnectionId),
                SignalCommand.AddCommandSuffix(connectionId)
            };
        }

        /// <summary>
        /// Called when a new connection is made.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="connectionId">The id of the connecting client.</param>
        /// <returns>A <see cref="Task"/> that completes when the connect operation is complete.</returns>
        protected virtual Task OnConnectedAsync(IRequest request, string connectionId)
        {
            OnClientConnected(connectionId);
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when a connection reconnects after a timeout.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="groups">The groups the calling connection is a part of.</param>
        /// <param name="connectionId">The id of the re-connecting client.</param>
        /// <returns>A <see cref="Task"/> that completes when the re-connect operation is complete.</returns>
        protected virtual Task OnReconnectedAsync(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when data is received from a connection.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/> for the current connection.</param>
        /// <param name="connectionId">The id of the connection sending the data.</param>
        /// <param name="data">The payload sent to the connection.</param>
        /// <returns>A <see cref="Task"/> that completes when the receive operation is complete.</returns>
        protected virtual Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            OnReceiving();
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when a connection disconnects.
        /// </summary>
        /// <param name="connectionId">The id of the disconnected connection.</param>
        /// <returns>A <see cref="Task"/> that completes when the disconnect operation is complete.</returns>
        protected virtual Task OnDisconnectAsync(string connectionId)
        {
            OnClientDisconnected(connectionId);
            return TaskAsyncHelper.Empty;
        }

        /// <summary>
        /// Called when there's an error on the connection.
        /// </summary>
        /// <param name="error">The <see cref="Exception"/> that occurred.</param>
        /// <returns>A <see cref="Task"/> that completes when the error operation is complete.</returns>
        protected virtual Task OnErrorAsync(Exception error)
        {
            return TaskAsyncHelper.Empty;
        }

        private Task ProcessNegotiationRequest(HostContext context)
        {
            var payload = new
            {
                Url = context.Request.Url.LocalPath.Replace("/negotiate", ""),
                ConnectionId = _connectionIdGenerator.GenerateConnectionId(context.Request),
                TryWebSockets = _transportManager.SupportsTransport(WebSocketsTransportName) && context.SupportsWebSockets(),
                WebSocketServerUrl = context.WebSocketServerUrl(),
                ProtocolVersion = "1.0"
            };

            if (!String.IsNullOrEmpty(context.Request.QueryString["callback"]))
            {
                return ProcessJsonpNegotiationRequest(context, payload);
            }

            context.Response.ContentType = Json.MimeType;
            return context.Response.EndAsync(_jsonSerializer.Stringify(payload));
        }

        private Task ProcessJsonpNegotiationRequest(HostContext context, object payload)
        {
            context.Response.ContentType = Json.JsonpMimeType;
            var data = Json.CreateJsonpCallback(context.Request.QueryString["callback"], _jsonSerializer.Stringify(payload));

            return context.Response.EndAsync(data);
        }

        private bool IsNegotiationRequest(IRequest request)
        {
            return request.Url.LocalPath.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private ITransport GetTransport(HostContext context)
        {
            return _transportManager.GetTransport(context);
        }

        private static void OnReceiving()
        {
            if (Receiving != null)
            {
                Receiving();
            }
        }

        private static void OnClientConnected(string id)
        {
            if (Connected != null)
            {
                Connected(id);
            }
        }

        private static void OnClientDisconnected(string id)
        {
            if (Disconnected != null)
            {
                Disconnected(id);
            }
        }
    }
}