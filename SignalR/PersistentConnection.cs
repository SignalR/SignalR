using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SignalR.Infrastructure;
using SignalR.Transports;
using SignalR.Web;

namespace SignalR
{
    public abstract class PersistentConnection : HttpTaskAsyncHandler, IGroupManager
    {
        private readonly Signaler _signaler;
        private readonly IMessageStore _store;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IConnectionIdFactory _connectionIdFactory;
        private readonly Lazy<bool> _hasAcceptWebSocketRequest =
            new Lazy<bool>(() => typeof(HttpContextBase).GetMethod("AcceptWebSocketRequest") != null);

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

        public override bool IsReusable
        {
            get
            {
                return false;
            }
        }

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

        private bool ClientShouldTryWebSockets
        {
            get
            {
                return _hasAcceptWebSocketRequest.Value;
            }
        }

        public override Task ProcessRequestAsync(HttpContextBase context)
        {
            Task task = null;
            
            if (IsNegotiationRequest(context.Request))
            {
                context.Response.ContentType = Json.MimeType;
                return context.Response.WriteAsync(_jsonSerializer.Stringify(new
                {
                    Url = VirtualPathUtility.ToAbsolute(context.Request.AppRelativeCurrentExecutionFilePath.Replace("/negotiate", "")),
                    ConnectionId = _connectionIdFactory.CreateConnectionId(context),
                    TryWebSockets = ClientShouldTryWebSockets
                }));
            }
            else
            {
                _transport = GetTransport(context);

                string connectionId = _transport.ConnectionId;

                // If there's no connection id then this is a bad request
                if (String.IsNullOrEmpty(connectionId))
                {
                    throw new InvalidOperationException("Protocol error: Missing connection id.");
                }

                IEnumerable<string> groups = GetGroups(context);

                Connection = CreateConnection(connectionId, groups, context);

                // Wire up the events we need
                _transport.Connected += () =>
                {
                    task = OnConnectedAsync(context, connectionId);
                };

                _transport.Received += (data) =>
                {
                    task = OnReceivedAsync(connectionId, data);
                };

                _transport.Error += (e) =>
                {
                    task = OnErrorAsync(e);
                };

                _transport.Disconnected += () =>
                {
                    task = OnDisconnectAsync(connectionId);
                };

                Func<Task> processRequestTask = _transport.ProcessRequest(Connection);

                if (processRequestTask != null)
                {
                    if (task != null)
                    {
                        return task.Then(processRequestTask).FastUnwrap();
                    }
                    return processRequestTask();
                }
            }

            return task ?? TaskAsyncHelper.Empty;
        }

        protected virtual IConnection CreateConnection(string connectionId, IEnumerable<string> groups, HttpContextBase context)
        {
            string groupValue = context.Request.QueryStringOrForm("groups") ?? String.Empty;

            // The list of default signals this connection cares about:
            // 1. The default signal (the type name)
            // 2. The connection id (so we can message this particular connection)
            // 3. connection id + SIGNALRCOMMAND -> for built in commands that we need to process
            var signals = new string[] {
                DefaultSignal,
                connectionId,
                connectionId + "." + SignalCommand.SignalrCommand
            };

            return new Connection(_store, _jsonSerializer, _signaler, DefaultSignal, connectionId, signals, groups);
        }

        protected virtual void OnConnected(HttpContextBase context, string connectionId) { }

        protected virtual Task OnConnectedAsync(HttpContextBase context, string connectionId)
        {
            OnClientConnected(connectionId);
            OnConnected(context, connectionId);
            return TaskAsyncHelper.Empty;
        }

        protected virtual void OnReceived(string connectionId, string data) { }

        protected virtual Task OnReceivedAsync(string connectionId, string data)
        {
            OnReceiving();
            OnReceived(connectionId, data);
            return TaskAsyncHelper.Empty;
        }

        protected virtual void OnDisconnect(string connectionId) { }

        protected virtual Task OnDisconnectAsync(string connectionId)
        {
            OnClientDisconnected(connectionId);
            OnDisconnect(connectionId);
            return TaskAsyncHelper.Empty;
        }

        protected virtual void OnError(Exception e) { }

        protected virtual Task OnErrorAsync(Exception e)
        {
            OnError(e);
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

        private string CreateQualifiedName(string groupName)
        {
            return DefaultSignal + "." + groupName;
        }

        private IEnumerable<string> GetGroups(HttpContextBase context)
        {
            string groupValue = context.Request.QueryStringOrForm("groups");

            if (String.IsNullOrEmpty(groupValue))
            {
                return Enumerable.Empty<string>();
            }

            return groupValue.Split(',');
        }

        private bool IsNegotiationRequest(HttpRequestBase httpRequest)
        {
            return httpRequest.Path.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private ITransport GetTransport(HttpContextBase context)
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