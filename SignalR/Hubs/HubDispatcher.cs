namespace SignalR.Hubs
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    using SignalR.Hosting;

    using System.Reflection;

    using SignalR.Hubs.Lookup;
    using SignalR.Hubs.Lookup.Descriptors;

    public class HubDispatcher : PersistentConnection
    {
        private IJavaScriptProxyGenerator _proxyGenerator;
        private IHubManager _manager;
        private HostContext _context;

        private readonly string _url;

        public HubDispatcher(string url)
        {
            this._url = url;
        }

        public override void Initialize(IDependencyResolver resolver)
        {
            this._proxyGenerator = resolver.Resolve<IJavaScriptProxyGenerator>();
            this._manager = resolver.Resolve<IHubManager>();

            base.Initialize(resolver);
        }

        protected override Task OnReceivedAsync(string connectionId, string data)
        {
            var hubRequest = JsonConvert.DeserializeObject<HubRequest>(data);

            // Create the hub
            var descriptor = this._manager.GetHub(hubRequest.Hub);

            // Deserialize the parameter name value pairs so we can match it up with the method's parameters
            var parameters = hubRequest.Data;

            // Resolve the action
            ActionDescriptor actionDescriptor = this._manager.GetHubAction(descriptor.Name, hubRequest.Action, parameters);

            if (actionDescriptor == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' could not be resolved.", hubRequest.Action));
            }

            // Resolving the actual state object
            var state = new TrackingDictionary(hubRequest.State);
            var hub = this.CreateHub(descriptor, connectionId, state);
            Task resultTask;

            try
            {
                // Invoke the action
                // pszmyd: Invoker delegate automatically adjusts JSON parameters to correct types.
                object result = actionDescriptor.Invoker(hub, parameters);
                Type returnType = result != null ? result.GetType() : actionDescriptor.ReturnType;

                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    var task = (Task)result;
                    if (!returnType.IsGenericType)
                    {
                        return task.ContinueWith(t => this.ProcessResult(state, null, hubRequest, t.Exception))
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
                    resultTask = this.ProcessResult(state, result, hubRequest, null);
                }
            }
            catch (TargetInvocationException e)
            {
                resultTask = this.ProcessResult(state, null, hubRequest, e);
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
                return context.Response.EndAsync(this._proxyGenerator.GenerateProxy(this._url));
            }

            this._context = context;

            return base.ProcessRequestAsync(context);
        }

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return this.ExecuteHubEventAsync<IConnected>(connectionId, hub => hub.Connect());
        }

        protected override Task OnReconnectedAsync(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return this.ExecuteHubEventAsync<IConnected>(connectionId, hub => hub.Reconnect(groups));
        }

        protected override Task OnDisconnectAsync(string connectionId)
        {
            return this.ExecuteHubEventAsync<IDisconnect>(connectionId, hub => hub.Disconnect());
        }

        private Task ExecuteHubEventAsync<T>(string connectionId, Func<T, Task> action) where T : class
        {
            var operations = this.GetHubsImplementingInterface(typeof(T))
                .Select(hub => this.CreateHub(hub, connectionId))
                .OfType<T>()
                .Select(instance => action(instance) ?? TaskAsyncHelper.Empty)
                .ToList();

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

        public IHub CreateHub(HubDescriptor descriptor, string connectionId, TrackingDictionary state = null)
        {
            var hub = this._manager.ResolveHub(descriptor.Name);

            if (hub != null)
            {
                state = state ?? new TrackingDictionary();
                hub.Context = new HubContext(this._context, connectionId);
                hub.Caller = new SignalAgent(this.Connection, connectionId, descriptor.Name, state);
                var agent = new ClientAgent(this.Connection, descriptor.Name);
                hub.Agent = agent;
                hub.GroupManager = agent;
            }

            return hub;
        }

        private IEnumerable<HubDescriptor> GetHubsImplementingInterface(Type interfaceType)
        {
            // Get hubs that implement the specified interface
            return this._manager.GetHubs(d => interfaceType.IsAssignableFrom(d.Type));
        }

        private Task ProcessTaskResult<T>(TrackingDictionary state, HubRequest request, Task<T> task)
        {
            if (task.IsFaulted)
            {
                return this.ProcessResult(state, null, request, task.Exception);
            }
            return this.ProcessResult(state, task.Result, request, null);
        }

        private Task ProcessResult(TrackingDictionary state, object result, HubRequest request, Exception error)
        {
            var exception = error != null ? error.GetBaseException() : null;
            string stackTrace = (exception != null && this._context.IsDebuggingEnabled()) ? exception.StackTrace : null;
            string errorMessage = exception != null ? exception.Message : null;

            var hubResult = new HubResult
            {
                State = state.GetChanges(),
                Result = result,
                Id = request.Id,
                Error = errorMessage,
                StackTrace = stackTrace
            };

            return this.Send(hubResult);
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

            IEnumerable<string> hubSignals = clientHubInfo.SelectMany(info => this.GetSignals(info, connectionId))
                                                          .Concat(this.GetDefaultSignals(connectionId));

            return new Connection(this._messageBus, this._jsonSerializer, null, connectionId, hubSignals, groups, this._trace);
        }

        private IEnumerable<string> GetSignals(ClientHubInfo hubInfo, string connectionId)
        {
            var clientSignals = new[] { 
                hubInfo.CreateQualifiedName(connectionId),
                SignalCommand.AddCommandSuffix(hubInfo.CreateQualifiedName(connectionId))
            };

            // Try to find the associated hub type
            var hub = this._manager.GetHub(hubInfo.Name);

            if (hub == null)
            {
                throw new InvalidOperationException(String.Format("Unable to resolve hub {0}.", hubInfo.Name));
            }

            // Set the full type name
            hubInfo.Name = hub.Name;

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
                return this.Name + "." + unqualifiedName;
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