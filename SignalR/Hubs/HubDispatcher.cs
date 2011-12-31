using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using SignalR.Abstractions;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class HubDispatcher : PersistentConnection
    {
        // We don't use the configured IJson serializer here because we assume things about
        // the parsed output. Doesn't matter coming in anyway as member resolution is 
        // case insensitive.
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer
        {
            MaxJsonLength = 30 * 1024 * 1024
        };

        private readonly Signaler _signaler;
        private readonly IMessageStore _store;
        private readonly IHubFactory _hubFactory;
        private readonly IActionResolver _actionResolver;
        private readonly IJavaScriptProxyGenerator _proxyGenerator;
        private readonly string _url;
        private readonly IHubLocator _hubLocator;
        private readonly IHubTypeResolver _hubTypeResolver;
        private readonly IJsonSerializer _jsonSerializer;

        private HostContext _context;

        public HubDispatcher(string url)
            : this(DependencyResolver.Resolve<IHubFactory>(),
                   DependencyResolver.Resolve<IMessageStore>(),
                   Signaler.Instance,
                   DependencyResolver.Resolve<IConnectionIdFactory>(),
                   DependencyResolver.Resolve<IActionResolver>(),
                   DependencyResolver.Resolve<IJavaScriptProxyGenerator>(),
                   DependencyResolver.Resolve<IJsonSerializer>(),
                   DependencyResolver.Resolve<IHubLocator>(),
                   DependencyResolver.Resolve<IHubTypeResolver>(),
                   url)
        {
        }

        public HubDispatcher(IHubFactory hubFactory,
                             IMessageStore store,
                             Signaler signaler,
                             IConnectionIdFactory connectionIdFactory,
                             IActionResolver actionResolver,
                             IJavaScriptProxyGenerator proxyGenerator,
                             IJsonSerializer jsonSerializer,
                             IHubLocator hubLocator,
                             IHubTypeResolver hubTypeResolver,
                             string url)
            : base(signaler, connectionIdFactory, store, jsonSerializer)
        {
            _hubFactory = hubFactory;
            _store = store;
            _jsonSerializer = jsonSerializer;
            _signaler = signaler;
            _actionResolver = actionResolver;
            _proxyGenerator = proxyGenerator;
            _hubLocator = hubLocator;
            _hubTypeResolver = hubTypeResolver;
            _url = url;
        }

        protected override Task OnReceivedAsync(string connectionId, string data)
        {
            var hubRequest = _serializer.Deserialize<HubRequest>(data);

            // Create the hub
            IHub hub = _hubFactory.CreateHub(hubRequest.Hub);

            // Deserialize the parameter name value pairs so we can match it up with the method's parameters
            var parameters = hubRequest.Data;

            // Resolve the action
            ActionInfo actionInfo = _actionResolver.ResolveAction(hub.GetType(), hubRequest.Action, parameters);

            if (actionInfo == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' could not be resolved.", hubRequest.Action));
            }

            string hubName = hub.GetType().FullName;

            var state = new TrackingDictionary(hubRequest.State);
            hub.Context = new HubContext(_context, connectionId);
            hub.Caller = new SignalAgent(Connection, connectionId, hubName, state);
            var agent = new ClientAgent(Connection, hubName);
            hub.Agent = agent;
            hub.GroupManager = agent;
            Task resultTask;

            try
            {
                // Execute the method
                object result = actionInfo.Method.Invoke(hub, actionInfo.Arguments);
                Type returnType = result != null ? result.GetType() : actionInfo.Method.ReturnType;

                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    var task = (Task)result;
                    if (!returnType.IsGenericType)
                    {
                        return task.ContinueWith(t => ProcessResult(state, null, hubRequest, t.Exception))
                            .FastUnwrap();
                    }
                    else
                    {
                        // Get the <T> in Task<T>
                        Type resultType = returnType.GetGenericArguments().Single();

                        // Get the correct ContinueWith overload
                        var continueWith = TaskAsyncHelper.GetContinueWith(task.GetType());

                        var taskParameter = Expression.Parameter(continueWith.Type);
                        var processResultMethod = typeof(HubDispatcher).GetMethod("ProcessResult", BindingFlags.NonPublic | BindingFlags.Instance);
                        var taskResult = Expression.Property(taskParameter, "Result");
                        var taskException = Expression.Property(taskParameter, "Exception");

                        var body = Expression.Call(Expression.Constant(this),
                                                   processResultMethod,
                                                   Expression.Constant(state),
                                                   Expression.Convert(taskResult, typeof(object)),
                                                   Expression.Constant(hubRequest),
                                                   Expression.Convert(taskException, typeof(Exception)));

                        var lambda = Expression.Lambda(body, taskParameter);

                        var call = Expression.Call(Expression.Constant(task, continueWith.Type), continueWith.Method, lambda);
                        return Expression.Lambda<Func<Task<Task>>>(call).Compile()().FastUnwrap();
                    }
                }
                else
                {
                    resultTask = ProcessResult(state, result, hubRequest, null);
                }
            }
            catch (TargetInvocationException e)
            {
                resultTask = ProcessResult(state, null, hubRequest, e);
            }

            return resultTask
                .ContinueWith(_ => base.OnReceivedAsync(connectionId, data))
                .FastUnwrap();
        }

        public override Task ProcessRequestAsync(HostContext context)
        {
            // Generate the proxy
            if (context.Request.Url.LocalPath.EndsWith("/hubs", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "application/x-javascript";
                return context.Response.WriteAsync(_proxyGenerator.GenerateProxy(_url));
            }

            _context = context;

            return base.ProcessRequestAsync(context);
        }

        protected override Task OnDisconnectAsync(string connectionId)
        {
            // Loop over each hub and call disconnect (if the hub supports it)
            foreach (Type type in GetDisconnectTypes())
            {
                string hubName = type.FullName;
                IHub hub = _hubFactory.CreateHub(hubName);

                var disconnect = hub as IDisconnect;
                if (disconnect != null)
                {
                    // REVIEW: We don't have any client state here since we're calling this from the server.
                    // Will this match user expectations?
                    var state = new TrackingDictionary();
                    hub.Context = new HubContext(_context, connectionId);
                    hub.Caller = new SignalAgent(Connection, connectionId, hubName, state);
                    var agent = new ClientAgent(Connection, hubName);
                    hub.Agent = agent;
                    hub.GroupManager = agent;

                    disconnect.Disconnect();
                }
            }

            return TaskAsyncHelper.Empty;
        }

        private IEnumerable<Type> GetDisconnectTypes()
        {
            // Get types that implement IDisconnect
            return from type in _hubLocator.GetHubs()
                   where typeof(IDisconnect).IsAssignableFrom(type)
                   select type;
        }

        private Task ProcessResult(TrackingDictionary state, object result, HubRequest request, Exception error)
        {
            var hubResult = new HubResult
            {
                State = state.GetChanges(),
                Result = result,
                Id = request.Id,
                Error = error != null ? error.GetBaseException().Message : null
            };

            return Send(hubResult);
        }

        protected override IConnection CreateConnection(string connectionId, IEnumerable<string> groups, IRequest request)
        {
            string data = request.QueryStringOrForm("connectionData");

            if (String.IsNullOrEmpty(data))
            {
                return base.CreateConnection(connectionId, groups, request);
            }

            var clientHubInfo = _serializer.Deserialize<IEnumerable<ClientHubInfo>>(data);

            if (clientHubInfo == null || !clientHubInfo.Any())
            {
                return base.CreateConnection(connectionId, groups, request);
            }

            IEnumerable<string> hubSignals = clientHubInfo.SelectMany(info => GetSignals(info, connectionId));

            return new Connection(_store, _jsonSerializer, _signaler, null, connectionId, hubSignals, groups);
        }

        private IEnumerable<string> GetSignals(ClientHubInfo hubInfo, string connectionId)
        {
            var clientSignals = new[] { 
                hubInfo.CreateQualifiedName(connectionId),
                SignalCommand.AddCommandSuffix(hubInfo.CreateQualifiedName(connectionId))
            };

            // Try to find the associated hub type
            Type hubType = _hubTypeResolver.ResolveType(hubInfo.Name);

            if (hubType == null)
            {
                throw new InvalidOperationException(String.Format("Unable to resolve hub {0}.", hubInfo.Name));
            }

            // Set the full type name
            hubInfo.Name = hubType.FullName;

            // Create the signals for hubs
            return hubInfo.Methods.Select(hubInfo.CreateQualifiedName)
                                  .Concat(clientSignals);

        }

        private class ClientHubInfo
        {
            public string Name { get; set; }
            public string[] Methods { get; set; }

            public string CreateQualifiedName(string unqualifiedName)
            {
                return Name + "." + unqualifiedName;
            }
        }

        private class HubResult
        {
            public IDictionary<string, object> State { get; set; }
            public object Result { get; set; }
            public string Id { get; set; }
            public string Error { get; set; }
        }

        private class HubRequest
        {
            public string Hub { get; set; }
            public string Action { get; set; }
            public object[] Data { get; set; }
            public IDictionary<string, object> State { get; set; }
            public string Id { get; set; }
        }
    }
}