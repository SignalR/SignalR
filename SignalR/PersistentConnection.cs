using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Abstractions;
using SignalR.Infrastructure;
using SignalR.Transports;

namespace SignalR
{
    public abstract class PersistentConnection : IGroupManager
    {
        private readonly Signaler _signaler;
        private readonly IMessageStore _store;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IConnectionIdFactory _connectionIdFactory;

        protected ITransport _transport;

        protected PersistentConnection()
            : this(Signaler.Instance,
                   DependencyResolver.Resolve<IConnectionIdFactory>(),
                   DependencyResolver.Resolve<IMessageStore>(),
                   DependencyResolver.Resolve<IJsonSerializer>())
        {
        }

        protected PersistentConnection(Signaler signaler,
                                       IConnectionIdFactory connectionIdFactory,
                                       IMessageStore store,
                                       IJsonSerializer jsonSerializer)
        {
            _signaler = signaler;
            _connectionIdFactory = connectionIdFactory;
            _store = store;
            _jsonSerializer = jsonSerializer;
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
            Task transportEventTask = null;

            if (IsNegotiationRequest(context.Request))
            {
                return ProcessNegotiationRequest(context);
            }

            _transport = GetTransport(context);

            string connectionId = _transport.ConnectionId;

            // If there's no connection id then this is a bad request
            if (String.IsNullOrEmpty(connectionId))
            {
                throw new InvalidOperationException("Protocol error: Missing connection id.");
            }

            IEnumerable<string> groups = _transport.Groups;

            Connection = CreateConnection(connectionId, groups, context.Request);

            // Wire up the events we need
            _transport.Connected += () =>
            {
                transportEventTask = OnConnectedAsync(context.Request, connectionId);
            };

            _transport.Received += (data) =>
            {
                transportEventTask = OnReceivedAsync(connectionId, data);
            };

            _transport.Error += (e) =>
            {
                transportEventTask = OnErrorAsync(e);
            };

            _transport.Disconnected += () =>
            {
                transportEventTask = OnDisconnectAsync(connectionId);
            };

            Func<Task> transportProcessRequest = _transport.ProcessRequest(Connection);

            if (transportProcessRequest != null)
            {
                if (transportEventTask != null)
                {
                    return transportEventTask.Then(transportProcessRequest).FastUnwrap();
                }
                return transportProcessRequest();
            }

            return transportEventTask ?? TaskAsyncHelper.Empty;
        }

        protected virtual IConnection CreateConnection(string connectionId, IEnumerable<string> groups, IRequest request)
        {
            // The list of default signals this connection cares about:
            // 1. The default signal (the type name)
            // 2. The connection id (so we can message this particular connection)
            // 3. connection id + SIGNALRCOMMAND -> for built in commands that we need to process
            var signals = new string[] {
                DefaultSignal,
                connectionId,
                SignalCommand.AddCommandSuffix(connectionId)
            };

            return new Connection(_store, _jsonSerializer, _signaler, DefaultSignal, connectionId, signals, groups);
        }

        protected virtual Task OnConnectedAsync(IRequest request, string connectionId)
        {
            OnClientConnected(connectionId);
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
            return _transport.Send(value);
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
            context.Response.ContentType = Json.MimeType;
            return context.Response.WriteAsync(_jsonSerializer.Stringify(new
            {
                Url = context.Request.Url.LocalPath.Replace("/negotiate", ""),
                ConnectionId = _connectionIdFactory.CreateConnectionId(context.Request),
                TryWebSockets = context.SupportsWebSockets()
            }));
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
            return TransportManager.GetTransport(context) ??
                new LongPollingTransport(context, _jsonSerializer);
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