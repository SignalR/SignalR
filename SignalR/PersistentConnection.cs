using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Hosting;
using SignalR.Infrastructure;
using SignalR.Transports;

namespace SignalR
{
    public abstract class PersistentConnection : IGroupManager
    {
        protected IMessageBus _messageBus;
        protected IJsonSerializer _jsonSerializer;
        protected IConnectionIdFactory _connectionIdFactory;
        private ITransportManager _transportManager;
        private bool _initialized;

        protected ITraceManager _trace;
        protected ITransport _transport;

        public virtual void Initialize(IDependencyResolver resolver)
        {
            if (_initialized)
            {
                return;
            }

            _messageBus = resolver.Resolve<IMessageBus>();
            _connectionIdFactory = resolver.Resolve<IConnectionIdFactory>();
            _jsonSerializer = resolver.Resolve<IJsonSerializer>();
            _transportManager = resolver.Resolve<ITransportManager>();
            _trace = resolver.Resolve<ITraceManager>();

            _initialized = true;
        }

        // Static events intended for use when measuring performance
        public static event Action Sending;
        public static event Action Receiving;
        public static event Action<string> ClientConnected;
        public static event Action<string> ClientDisconnected;

        public IConnection Connection
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

            Connection = CreateConnection(connectionId, groups, context.Request);

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
                return OnReceivedAsync(connectionId, data);
            };

            _transport.Error = OnErrorAsync;

            _transport.Disconnected = () =>
            {
                return OnDisconnectAsync(connectionId);
            };

            return _transport.ProcessRequest(Connection) ?? TaskAsyncHelper.Empty;
        }

        protected virtual IConnection CreateConnection(string connectionId, IEnumerable<string> groups, IRequest request)
        {
            return new Connection(_messageBus,
                                  _jsonSerializer,
                                  DefaultSignal,
                                  connectionId,
                                  GetDefaultSignals(connectionId),
                                  groups,
                                  _trace);
        }

        protected IEnumerable<string> GetDefaultSignals(string connectionId)
        {
            // The list of default signals this connection cares about:
            // 1. The default signal (the type name)
            // 2. The connection id (so we can message this particular connection)
            // 3. connection id + SIGNALRCOMMAND -> for built in commands that we need to process
            return new string[] {
                DefaultSignal,
                connectionId,
                SignalCommand.AddCommandSuffix(connectionId)
            };
        }

        protected virtual Task OnConnectedAsync(IRequest request, string connectionId)
        {
            OnClientConnected(connectionId);
            return TaskAsyncHelper.Empty;
        }

        protected virtual Task OnReconnectedAsync(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return TaskAsyncHelper.Empty;
        }

        protected virtual Task OnReceivedAsync(string connectionId, string data)
        {
            OnReceiving();
            return TaskAsyncHelper.Empty;
        }

        protected virtual Task OnDisconnectAsync(string connectionId)
        {
            OnClientDisconnected(connectionId);
            return TaskAsyncHelper.Empty;
        }

        protected virtual Task OnErrorAsync(Exception e)
        {
            return TaskAsyncHelper.Empty;
        }

        public Task Send(object value)
        {
            OnSending();
            return Connection.Send(value);
        }

        public Task Send(string connectionId, object value)
        {
            OnSending();
            return Connection.Broadcast(connectionId, value);
        }

        public Task SendToGroup(string groupName, object value)
        {
            OnSending();
            return Connection.Broadcast(CreateQualifiedName(groupName), value);
        }

        public Task AddToGroup(string connectionId, string groupName)
        {
            groupName = CreateQualifiedName(groupName);
            return Connection.SendCommand(new SignalCommand
            {
                Type = CommandType.AddToGroup,
                Value = groupName
            });
        }

        public Task RemoveFromGroup(string connectionId, string groupName)
        {
            groupName = CreateQualifiedName(groupName);
            return Connection.SendCommand(new SignalCommand
            {
                Type = CommandType.RemoveFromGroup,
                Value = groupName
            });
        }

        private Task ProcessNegotiationRequest(HostContext context)
        {
            var payload = new
            {
                Url = context.Request.Url.LocalPath.Replace("/negotiate", ""),
                ConnectionId = _connectionIdFactory.CreateConnectionId(context.Request, context.User),
                TryWebSockets = context.SupportsWebSockets(),
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

        private string CreateQualifiedName(string groupName)
        {
            return DefaultSignal + "." + groupName;
        }

        private bool IsNegotiationRequest(IRequest request)
        {
            return request.Url.LocalPath.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private ITransport GetTransport(HostContext context)
        {
            return _transportManager.GetTransport(context);
        }

        private static void OnSending()
        {
            if (Sending != null)
            {
                Sending();
            }
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
            if (ClientConnected != null)
            {
                ClientConnected(id);
            }
        }

        private static void OnClientDisconnected(string id)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(id);
            }
        }
    }
}