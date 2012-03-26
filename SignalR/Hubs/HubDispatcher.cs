using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SignalR.Hosting;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class HubDispatcher : PersistentConnection
    {
        private IHubFactory _hubFactory;
        private IActionResolver _actionResolver;
        private IJavaScriptProxyGenerator _proxyGenerator;
        private IHubLocator _hubLocator;
        private IHubTypeResolver _hubTypeResolver;
        private HostContext _context;

        private readonly string _url;

        public HubDispatcher(string url)
        {
            _url = url;
        }

        public override void Initialize(IDependencyResolver resolver)
        {
            _hubFactory = resolver.Resolve<IHubFactory>();
            _actionResolver = resolver.Resolve<IActionResolver>();
            _proxyGenerator = resolver.Resolve<IJavaScriptProxyGenerator>();
            _hubLocator = resolver.Resolve<IHubLocator>();
            _hubTypeResolver = resolver.Resolve<IHubTypeResolver>();

            base.Initialize(resolver);
        }

        protected override Task OnReceivedAsync(string connectionId, string data)
        {
            var hubRequest = JsonConvert.DeserializeObject<HubRequest>(data);

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
                        var processResultMethod = typeof(HubDispatcher).GetMethod("ProcessTaskResult", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(resultType);

                        var body = Expression.Call(Expression.Constant(this),
                                                   processResultMethod,
                                                   Expression.Constant(state),
                                                   Expression.Constant(hubRequest),
                                                   taskParameter);

                        var lambda = Expression.Lambda(body, taskParameter);

                        var call = Expression.Call(Expression.Constant(task, continueWith.Type), continueWith.Method, lambda);
                        Func<Task<Task>> continueWithMethod = Expression.Lambda<Func<Task<Task>>>(call).Compile();
                        return continueWithMethod.Invoke().FastUnwrap();
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
                return context.Response.EndAsync(_proxyGenerator.GenerateProxy(_url));
            }

            _context = context;

            return base.ProcessRequestAsync(context);
        }

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return ExecuteHubEventAsync<IConnected>(connectionId, hub => hub.Connect());
        }

        protected override Task OnReconnectedAsync(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return ExecuteHubEventAsync<IConnected>(connectionId, hub => hub.Reconnect(groups));
        }

        protected override Task OnDisconnectAsync(string connectionId)
        {
            return ExecuteHubEventAsync<IDisconnect>(connectionId, hub => hub.Disconnect());
        }

        private Task ExecuteHubEventAsync<T>(string connectionId, Func<T, Task> action) where T : class
        {
            var operations = new List<Task>();

            foreach (Type type in GetHubsImplementingInterface(typeof(T)))
            {
                T instance = CreateHub(type, connectionId) as T;
                if (instance != null)
                {
                    // Collect all the asyc operations
                    operations.Add(action(instance) ?? TaskAsyncHelper.Empty);
                }
            }

            if (operations.Count == 0)
            {
                return TaskAsyncHelper.Empty;
            }

            var tcs = new TaskCompletionSource<object>();
            Task.Factory.ContinueWhenAll(operations.ToArray(), tasks =>
            {
                var faulted = tasks.FirstOrDefault(t => t.IsFaulted);
                if (faulted != null)
                {
                    tcs.SetException(faulted.Exception);
                }
                else if (tasks.Any(t => t.IsCanceled))
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult(null);
                }
            });

            return tcs.Task;
        }

        public IHub CreateHub(Type type, string connectionId)
        {
            string hubName = type.FullName;
            IHub hub = _hubFactory.CreateHub(hubName);

            if (hub != null)
            {
                var state = new TrackingDictionary();
                hub.Context = new HubContext(_context, connectionId);
                hub.Caller = new SignalAgent(Connection, connectionId, hubName, state);
                var agent = new ClientAgent(Connection, hubName);
                hub.Agent = agent;
                hub.GroupManager = agent;
            }

            return hub;
        }

        private IEnumerable<Type> GetHubsImplementingInterface(Type interfaceType)
        {
            // Get hubs that implement the specified interface
            return from type in _hubLocator.GetHubs()
                   where interfaceType.IsAssignableFrom(type)
                   select type;
        }

        private Task ProcessTaskResult<T>(TrackingDictionary state, HubRequest request, Task<T> task)
        {
            if (task.IsFaulted)
            {
                return ProcessResult(state, null, request, task.Exception);
            }
            return ProcessResult(state, task.Result, request, null);
        }

        private Task ProcessResult(TrackingDictionary state, object result, HubRequest request, Exception error)
        {
            var exception = error != null ? error.GetBaseException() : null;
            string stackTrace = (exception != null && _context.IsDebuggingEnabled()) ? exception.StackTrace : null;
            string errorMessage = exception != null ? exception.Message : null;

            var hubResult = new HubResult
            {
                State = state.GetChanges(),
                Result = result,
                Id = request.Id,
                Error = errorMessage,
                StackTrace = stackTrace
            };

            return _transport.Send(hubResult);
        }

        protected override IConnection CreateConnection(string connectionId, IEnumerable<string> groups, IRequest request)
        {
            string data = request.QueryStringOrForm("connectionData");

            if (String.IsNullOrEmpty(data))
            {
                return base.CreateConnection(connectionId, groups, request);
            }

            var clientHubInfo = JsonConvert.DeserializeObject<IEnumerable<ClientHubInfo>>(data);

            if (clientHubInfo == null || !clientHubInfo.Any())
            {
                return base.CreateConnection(connectionId, groups, request);
            }

            IEnumerable<string> hubSignals = clientHubInfo.SelectMany(info => GetSignals(info, connectionId))
                                                          .Concat(GetDefaultSignals(connectionId));

            return new Connection(_messageBus, _jsonSerializer, null, connectionId, hubSignals, groups, _trace);
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
            public string StackTrace { get; set; }
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