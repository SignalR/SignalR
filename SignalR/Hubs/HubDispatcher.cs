using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    /// <summary>
    /// Handles all communication over the hubs persistent connection.
    /// </summary>
    public class HubDispatcher : PersistentConnection
    {
        private IJavaScriptProxyGenerator _proxyGenerator;
        private IHubManager _manager;
        private IHubRequestParser _requestParser;
        private IParameterResolver _binder;
        private readonly List<HubDescriptor> _hubs = new List<HubDescriptor>();
        private bool _isDebuggingEnabled;

        private readonly string _url;

        /// <summary>
        /// Initializes an instance of the <see cref="HubDispatcher"/> class.
        /// </summary>
        /// <param name="url">The base url of the connection url.</param>
        public HubDispatcher(string url)
        {
            _url = url;
        }

        protected override TraceSource Trace
        {
            get
            {
                return _trace["SignalR.HubDispatcher"];
            }
        }

        public override void Initialize(IDependencyResolver resolver)
        {
            _proxyGenerator = resolver.Resolve<IJavaScriptProxyGenerator>();
            _manager = resolver.Resolve<IHubManager>();
            _binder = resolver.Resolve<IParameterResolver>();
            _requestParser = resolver.Resolve<IHubRequestParser>();

            base.Initialize(resolver);
        }

        /// <summary>
        /// Processes the hub's incoming method calls.
        /// </summary>
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            HubRequest hubRequest = _requestParser.Parse(data);

            // Create the hub
            HubDescriptor descriptor = _manager.EnsureHub(hubRequest.Hub);

            IJsonValue[] parameterValues = hubRequest.ParameterValues;

            // Resolve the method
            MethodDescriptor methodDescriptor = _manager.GetHubMethod(descriptor.Name, hubRequest.Method, parameterValues);
            if (methodDescriptor == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' method could not be resolved.", hubRequest.Method));
            }

            // Resolving the actual state object
            var state = new TrackingDictionary(hubRequest.State);
            var hub = CreateHub(request, descriptor, connectionId, state, throwIfFailedToCreate: true);

            Task resultTask;

            try
            {
                // Invoke the method
                object result = methodDescriptor.Invoker.Invoke(hub, _binder.ResolveMethodParameters(methodDescriptor, parameterValues));
                Type returnType = result != null ? result.GetType() : methodDescriptor.ReturnType;

                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    var task = (Task)result;
                    if (!returnType.IsGenericType)
                    {
                        return task.ContinueWith(t => ProcessResponse(state, null, hubRequest, t.Exception))
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
                    resultTask = ProcessResponse(state, result, hubRequest, null);
                }
            }
            catch (TargetInvocationException e)
            {
                resultTask = ProcessResponse(state, null, hubRequest, e);
            }

            return resultTask
                .ContinueWith(_ => base.OnReceivedAsync(request, connectionId, data))
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

            _isDebuggingEnabled = context.IsDebuggingEnabled();

            return base.ProcessRequestAsync(context);
        }

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return ExecuteHubEventAsync<IConnected>(request, connectionId, hub => hub.Connect());
        }

        protected override Task OnReconnectedAsync(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return ExecuteHubEventAsync<IConnected>(request, connectionId, hub => hub.Reconnect(groups));
        }

        protected override Task OnDisconnectAsync(string connectionId)
        {
            return ExecuteHubEventAsync<IDisconnect>(request: null, connectionId: connectionId, action: hub => hub.Disconnect());
        }

        private Task ExecuteHubEventAsync<T>(IRequest request, string connectionId, Func<T, Task> action) where T : class
        {
            var operations = GetHubsImplementingInterface(typeof(T))
                .Select(hub => CreateHub(request, hub, connectionId))
                .OfType<T>()
                .Select(instance => action(instance).Catch() ?? TaskAsyncHelper.Empty)
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

        private IHub CreateHub(IRequest request, HubDescriptor descriptor, string connectionId, TrackingDictionary state = null, bool throwIfFailedToCreate = false)
        {
            try
            {
                var hub = _manager.ResolveHub(descriptor.Name);

                if (hub != null)
                {
                    state = state ?? new TrackingDictionary();
                    hub.Context = new HubCallerContext(request, connectionId);
                    hub.Caller = new StatefulSignalProxy(Connection, connectionId, descriptor.Name, state);
                    hub.Clients = new ClientProxy(Connection, descriptor.Name);
                    hub.Groups = new GroupManager(Connection, descriptor.Name);
                }

                return hub;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Error creating hub {0}. " + ex.Message, descriptor.Name);

                if (throwIfFailedToCreate)
                {
                    throw;
                }

                return null;
            }
        }

        private IEnumerable<HubDescriptor> GetHubsImplementingInterface(Type interfaceType)
        {
            // Get hubs that implement the specified interface
            return _hubs.Where(hub => interfaceType.IsAssignableFrom(hub.Type));
        }

        private Task ProcessTaskResult<T>(TrackingDictionary state, HubRequest request, Task<T> task)
        {
            if (task.IsFaulted)
            {
                return ProcessResponse(state, null, request, task.Exception);
            }
            return ProcessResponse(state, task.Result, request, null);
        }

        private Task ProcessResponse(TrackingDictionary state, object result, HubRequest request, Exception error)
        {
            var exception = error.Unwrap();
            string stackTrace = (exception != null && _isDebuggingEnabled) ? exception.StackTrace : null;
            string errorMessage = exception != null ? exception.Message : null;

            var hubResult = new HubResponse
            {
                State = state.GetChanges(),
                Result = result,
                Id = request.Id,
                Error = errorMessage,
                StackTrace = stackTrace
            };

            return _transport.Send(hubResult);
        }

        protected override Connection CreateConnection(string connectionId, IEnumerable<string> groups, IRequest request)
        {
            string data = request.QueryStringOrForm("connectionData");

            if (String.IsNullOrEmpty(data))
            {
                return base.CreateConnection(connectionId, groups, request);
            }

            var clientHubInfo = _jsonSerializer.Parse<IEnumerable<ClientHubInfo>>(data);

            if (clientHubInfo == null || !clientHubInfo.Any())
            {
                return base.CreateConnection(connectionId, groups, request);
            }

            IEnumerable<string> hubSignals = clientHubInfo.SelectMany(info => GetSignals(info, connectionId))
                                                          .Concat(GetDefaultSignals(connectionId));

            return new Connection(_messageBus, 
                                  _newMessageBus, 
                                  _jsonSerializer, 
                                  null, 
                                  connectionId, 
                                  hubSignals, 
                                  groups, 
                                  _trace);
        }

        private IEnumerable<string> GetSignals(ClientHubInfo hubInfo, string connectionId)
        {
            // Try to find the associated hub type
            HubDescriptor hubDescriptor = _manager.EnsureHub(hubInfo.Name);

            // Add this to the list of hub desciptors this connection is interested in
            _hubs.Add(hubDescriptor);

            // Update the name (Issue #344)
            hubInfo.Name = hubDescriptor.Name;

            // Create the signals for hubs
            // 1. The hub name e.g. MyHub
            // 2. The connection id for this hub e.g. MyHub.{guid}
            // 3. The command signal for this connection
            var clientSignals = new[] {
                hubInfo.Name,
                hubInfo.CreateQualifiedName(connectionId),
                SignalCommand.AddCommandSuffix(hubInfo.CreateQualifiedName(connectionId))
            };

            return clientSignals;
        }

        private class ClientHubInfo
        {
            public string Name { get; set; }

            public string CreateQualifiedName(string unqualifiedName)
            {
                return Name + "." + unqualifiedName;
            }
        }
    }
}