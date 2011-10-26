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
        internal const string SignalrCommand = "__SIGNALRCOMMAND__";

        private readonly Signaler _signaler;
        private readonly IMessageStore _store;
        private readonly IJsonStringifier _jsonStringifier;
        private readonly IClientIdFactory _clientIdFactory;

        protected ITransport _transport;

        protected PersistentConnection()
            : this(Signaler.Instance,
                   DependencyResolver.Resolve<IClientIdFactory>(),
                   DependencyResolver.Resolve<IMessageStore>(),
                   DependencyResolver.Resolve<IJsonStringifier>())
        {
        }

        protected PersistentConnection(Signaler signaler,
                                       IClientIdFactory clientIdFactory,
                                       IMessageStore store,
                                       IJsonStringifier jsonStringifier)
        {
            _signaler = signaler;
            _clientIdFactory = clientIdFactory;
            _store = store;
            _jsonStringifier = jsonStringifier;
        }

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

        public override Task ProcessRequestAsync(HttpContext context)
        {
            Task task = null;
            var contextBase = new HttpContextWrapper(context);

            if (IsNegotiationRequest(context.Request))
            {
                context.Response.ContentType = Json.MimeType;
                context.Response.Write(_jsonStringifier.Stringify(new
                {
                    Url = VirtualPathUtility.ToAbsolute(context.Request.AppRelativeCurrentExecutionFilePath.Replace("/negotiate", "")),
                    ClientId = _clientIdFactory.CreateClientId(contextBase)
                }));
            }
            else
            {
                _transport = GetTransport(contextBase);

                string clientId = contextBase.Request["clientId"];

                // If there's no client id then this is a bad request
                if (String.IsNullOrEmpty(clientId))
                {
                    throw new InvalidOperationException("Protcol error: Missing client id.");
                }

                IEnumerable<string> groups = GetGroups(contextBase);

                Connection = CreateConnection(clientId, groups, contextBase);

                // Wire up the events we need
                _transport.Connected += () =>
                {
                    task = OnConnectedAsync(contextBase, clientId);
                };

                _transport.Received += (data) =>
                {
                    task = OnReceivedAsync(clientId, data);
                };

                _transport.Error += (e) =>
                {
                    task = OnErrorAsync(e);
                };

                _transport.Disconnected += () =>
                {
                    task = OnDisconnectAsync(clientId);
                };

                Func<Task> processRequestTask = _transport.ProcessRequest(Connection);

                if (processRequestTask != null)
                {
                    if (task != null)
                    {
                        return task.Success(_ => processRequestTask()).Unwrap();
                    }
                    return processRequestTask();
                }
            }

            return task ?? TaskAsyncHelper.Empty;
        }

        protected virtual IConnection CreateConnection(string clientId, IEnumerable<string> groups, HttpContextBase context)
        {
            string groupValue = context.Request["groups"] ?? String.Empty;

            // The list of default signals this connection cares about:
            // 1. The default signal (the type name)
            // 2. The client id (so we can message this particular connection)
            // 3. client id + SIGNALRCOMMAND -> for built in commands that we need to process
            var signals = new string[] {
                DefaultSignal,
                clientId,
                clientId + "." + SignalrCommand
            };

            return new Connection(_store, _signaler, DefaultSignal, clientId, signals, groups);
        }

        protected virtual void OnConnected(HttpContextBase context, string clientId) { }

        protected virtual Task OnConnectedAsync(HttpContextBase context, string clientId)
        {
            OnConnected(context, clientId);
            return TaskAsyncHelper.Empty;
        }

        protected virtual void OnReceived(string clientId, string data) { }

        protected virtual Task OnReceivedAsync(string clientId, string data)
        {
            OnReceived(clientId, data);
            return TaskAsyncHelper.Empty;
        }

        protected virtual void OnDisconnect(string clientId) { }

        protected virtual Task OnDisconnectAsync(string clientId)
        {
            OnDisconnect(clientId);
            return TaskAsyncHelper.Empty;
        }

        protected virtual void OnError(Exception e) { }

        protected virtual Task OnErrorAsync(Exception e)
        {
            OnError(e);
            return TaskAsyncHelper.Empty;
        }

        public void Send(object value)
        {
            _transport.Send(value);
        }

        public Task Send(string clientId, object value)
        {
            return Connection.Broadcast(clientId, value);
        }

        public Task SendToGroup(string groupName, object value)
        {
            return Connection.Broadcast(CreateQualifiedName(groupName), value);
        }

        public Task AddToGroup(string clientId, string groupName)
        {
            groupName = CreateQualifiedName(groupName);
            return SendCommand(clientId, CommandType.AddToGroup, groupName);
        }

        public Task RemoveFromGroup(string clientId, string groupName)
        {
            groupName = CreateQualifiedName(groupName);
            return SendCommand(clientId, CommandType.RemoveFromGroup, groupName);
        }

        private Task SendCommand(string clientId, CommandType type, object value)
        {
            string signal = clientId + "." + SignalrCommand;

            var groupCommand = new SignalCommand
            {
                Type = type,
                Value = value
            };

            return Connection.Broadcast(signal, groupCommand);
        }

        private string CreateQualifiedName(string groupName)
        {
            return DefaultSignal + "." + groupName;
        }

        private IEnumerable<string> GetGroups(HttpContextBase context)
        {
            string groupValue = context.Request["groups"];

            if (String.IsNullOrEmpty(groupValue))
            {
                return Enumerable.Empty<string>();
            }

            return groupValue.Split(',');
        }

        private bool IsNegotiationRequest(HttpRequest httpRequest)
        {
            return httpRequest.Path.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase);
        }

        private ITransport GetTransport(HttpContextBase context)
        {
            return TransportManager.GetTransport(context) ??
                   new LongPollingTransport(context, _jsonStringifier);
        }
    }
}