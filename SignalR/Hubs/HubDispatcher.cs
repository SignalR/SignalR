using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using SignalR.Infrastructure;

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
        private IHubPipelineInvoker _pipelineInvoker;
        private readonly List<HubDescriptor> _hubs = new List<HubDescriptor>();
        private bool _isDebuggingEnabled;
        private PerformanceCounter _allErrorsTotalCounter;
        private PerformanceCounter _allErrorsPerSecCounter;
        private PerformanceCounter _hubInvocationErrorsTotalCounter;
        private PerformanceCounter _hubInvocationErrorsPerSecCounter;
        private PerformanceCounter _hubResolutionErrorsTotalCounter;
        private PerformanceCounter _hubResolutionErrorsPerSecCounter;
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
            _pipelineInvoker = resolver.Resolve<IHubPipelineInvoker>();

            var counters = resolver.Resolve<IPerformanceCounterWriter>();
            _allErrorsTotalCounter = counters.GetCounter(PerformanceCounters.ErrorsAllTotal);
            _allErrorsPerSecCounter = counters.GetCounter(PerformanceCounters.ErrorsAllPerSec);
            _hubInvocationErrorsTotalCounter = counters.GetCounter(PerformanceCounters.ErrorsHubInvocationTotal);
            _hubInvocationErrorsPerSecCounter = counters.GetCounter(PerformanceCounters.ErrorsHubInvocationPerSec);
            _hubResolutionErrorsTotalCounter = counters.GetCounter(PerformanceCounters.ErrorsHubResolutionTotal);
            _hubResolutionErrorsPerSecCounter = counters.GetCounter(PerformanceCounters.ErrorsHubResolutionPerSec);

            base.Initialize(resolver);
        }

        /// <summary>
        /// Processes the hub's incoming method calls.
        /// </summary>
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            HubRequest hubRequest = _requestParser.Parse(data);

            // Create the hub
            HubDescriptor descriptor = _manager.EnsureHub(hubRequest.Hub,
                _hubResolutionErrorsTotalCounter,
                _hubResolutionErrorsPerSecCounter,
                _allErrorsTotalCounter,
                _allErrorsPerSecCounter);

            IJsonValue[] parameterValues = hubRequest.ParameterValues;

            // Resolve the method
            MethodDescriptor methodDescriptor = _manager.GetHubMethod(descriptor.Name, hubRequest.Method, parameterValues);
            if (methodDescriptor == null)
            {
                _hubResolutionErrorsTotalCounter.SafeIncrement();
                _hubResolutionErrorsPerSecCounter.SafeIncrement();
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' method could not be resolved.", hubRequest.Method));
            }

            // Resolving the actual state object
            var state = new TrackingDictionary(hubRequest.State);
            var hub = CreateHub(request, descriptor, connectionId, state, throwIfFailedToCreate: true);

            return InvokeHubPipeline(request, connectionId, data, hubRequest, parameterValues, methodDescriptor, state, hub)
                .ContinueWith(task => hub.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
        }

        private Task InvokeHubPipeline(IRequest request, string connectionId, string data, HubRequest hubRequest, IJsonValue[] parameterValues, MethodDescriptor methodDescriptor, TrackingDictionary state, IHub hub)
        {

            var args = _binder.ResolveMethodParameters(methodDescriptor, parameterValues);
            var context = new HubInvokerContext(hub, state, methodDescriptor, args);

            // Invoke the pipeline
            return _pipelineInvoker.Invoke(context)
                                   .ContinueWith(task =>
                                   {
                                       if (task.IsFaulted)
                                       {
                                           return ProcessResponse(state, null, hubRequest, task.Exception);
                                       }
                                       else
                                       {
                                           return ProcessResponse(state, task.Result, hubRequest, null);
                                       }
                                   })
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

        internal static Task Connect(IHub hub)
        {
            return ((IConnected)hub).Connect();
        }

        internal static Task Reconnect(IHub hub)
        {
            return ((IConnected)hub).Reconnect();
        }

        internal static Task Disconnect(IHub hub)
        {
            return ((IDisconnect)hub).Disconnect();
        }

        internal static IEnumerable<string> RejoiningGroups(IHub hub, IEnumerable<string> groups)
        {
            return ((IConnected)hub).RejoiningGroups(groups);
        }

        internal static Task<object> Incoming(IHubIncomingInvokerContext context)
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                var result = context.MethodDescriptor.Invoker.Invoke(context.Hub, context.Args);
                Type returnType = context.MethodDescriptor.ReturnType;

                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    var task = (Task)result;
                    if (!returnType.IsGenericType)
                    {
                        task.ContinueWith(tcs);
                    }
                    else
                    {
                        // Get the <T> in Task<T>
                        Type resultType = returnType.GetGenericArguments().Single();

                        Type genericTaskType = typeof(Task<>).MakeGenericType(resultType);

                        // Get the correct ContinueWith overload
                        var parameter = Expression.Parameter(typeof(object));

                        // TODO: Cache this whole thing
                        // Action<object> callback = result => ContinueWith((Task<T>)result, tcs);
                        var continueWithMethod = typeof(HubDispatcher).GetMethod("ContinueWith", BindingFlags.NonPublic | BindingFlags.Static)
                                                                      .MakeGenericMethod(resultType);

                        Expression body = Expression.Call(continueWithMethod,
                                                          Expression.Convert(parameter, genericTaskType),
                                                          Expression.Constant(tcs));

                        var continueWithInvoker = Expression.Lambda<Action<object>>(body, parameter).Compile();
                        continueWithInvoker.Invoke(result);
                    }
                }
                else
                {
                    tcs.TrySetResult(result);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        private static void ContinueWith<T>(Task<T> task, TaskCompletionSource<object> tcs)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(t.Result);
                }
            });
        }

        internal static Task Outgoing(IHubOutgoingInvokerContext context)
        {
            return context.Connection.Send(context.Signal, context.Invocation);
        }

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return ExecuteHubEventAsync<IConnected>(request, connectionId, hub => _pipelineInvoker.Connect(hub));
        }

        protected override Task OnReconnectedAsync(IRequest request, string connectionId)
        {
            return ExecuteHubEventAsync<IConnected>(request, connectionId, hub => _pipelineInvoker.Reconnect(hub));
        }

        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return GetHubsImplementingInterface(typeof(IHub), request, connectionId)
                .Select(hub =>
                {
                    string groupPrefix = hub.GetType().Name + ".";
                    IEnumerable<string> groupsToRejoin =  _pipelineInvoker.RejoiningGroups(hub, groups.Where(g => g.StartsWith(groupPrefix))
                                                                                                      .Select(g => g.Substring(groupPrefix.Length)))
                                                                          .Select(g => groupPrefix + g).ToList();
                    hub.Dispose();
                    return groupsToRejoin;
                })
                .SelectMany(groupsToRejoin => groupsToRejoin);
        }

        protected override Task OnDisconnectAsync(string connectionId)
        {
            return ExecuteHubEventAsync<IDisconnect>(request: null, connectionId: connectionId, action: hub => _pipelineInvoker.Disconnect(hub));
        }

        private Task ExecuteHubEventAsync<T>(IRequest request, string connectionId, Func<IHub, Task> action) where T : class
        {
            var hubs = GetHubsImplementingInterface(typeof(T), request, connectionId);
            var operations = hubs.Select(instance => action(instance).Catch().OrEmpty()).ToArray();

            if (operations.Length == 0)
            {
                DisposeHubs(hubs);
                return TaskAsyncHelper.Empty;
            }

            var tcs = new TaskCompletionSource<object>();
            Task.Factory.ContinueWhenAll(operations, tasks =>
            {
                DisposeHubs(hubs);
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

                    Func<string, ClientHubInvocation, Task> send = (signal, value) => _pipelineInvoker.Send(new HubOutgoingInvokerContext(Connection, signal, value));

                    hub.Context = new HubCallerContext(request, connectionId);
                    hub.Caller = new StatefulSignalProxy(send, connectionId, descriptor.Name, state);
                    hub.Clients = new ClientProxy(send, descriptor.Name);
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

        private IEnumerable<IHub> GetHubsImplementingInterface(Type interfaceType, IRequest request, string connectionId)
        {
            // Get hubs that implement the specified interface
            return _hubs.Where(hubDescriptor => interfaceType.IsAssignableFrom(hubDescriptor.Type))
                        .Select(hub => CreateHub(request, hub, connectionId))
                        .Where(hub => hub != null).ToList();
        }

        private void DisposeHubs(IEnumerable<IHub> hubs)
        {
            foreach (var hub in hubs)
            {
                hub.Dispose();
            }
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

            if (exception != null)
            {
                _hubInvocationErrorsTotalCounter.SafeIncrement();
                _hubInvocationErrorsPerSecCounter.SafeIncrement();
                _allErrorsTotalCounter.SafeIncrement();
                _allErrorsPerSecCounter.SafeIncrement();
            }

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

        protected override Connection CreateConnection(string connectionId, IEnumerable<string> signals, IEnumerable<string> groups)
        {
            if (_hubs.Count > 0)
            {
                return new Connection(_newMessageBus, _jsonSerializer, null, connectionId, signals, groups, _trace, _ackHandler, _counters);
            }
            else
            {
                return base.CreateConnection(connectionId, signals, groups);
            }
        }

        private IEnumerable<string> GetHubSignals(ClientHubInfo hubInfo, string connectionId)
        {
            // Try to find the associated hub type
            HubDescriptor hubDescriptor = _manager.EnsureHub(hubInfo.Name,
                _hubResolutionErrorsTotalCounter,
                _hubResolutionErrorsPerSecCounter,
                _allErrorsTotalCounter,
                _allErrorsPerSecCounter);

            // Add this to the list of hub descriptors this connection is interested in
            _hubs.Add(hubDescriptor);

            // Update the name (Issue #344)
            hubInfo.Name = hubDescriptor.Name;

            // Create the signals for hubs
            // 1. The hub name e.g. MyHub
            // 2. The connection id for this hub e.g. MyHub.{guid}
            var clientSignals = new[] {
                hubInfo.Name,
                hubInfo.CreateQualifiedName(connectionId)
            };

            return clientSignals;
        }

        protected override IEnumerable<string> GetSignals(string connectionId, IRequest request)
        {
            string data = request.QueryStringOrForm("connectionData");

            if (String.IsNullOrEmpty(data))
            {
                return base.GetSignals(connectionId, request);
            }

            var clientHubInfo = _jsonSerializer.Parse<IEnumerable<ClientHubInfo>>(data);

            if (clientHubInfo == null || !clientHubInfo.Any())
            {
                base.GetSignals(connectionId, request);
            }

            return clientHubInfo.SelectMany(info => GetHubSignals(info, connectionId))
                                .Concat(base.GetSignals(connectionId, request)).ToList();
        }

        private class ClientHubInfo
        {
            public string Name { get; set; }

            public string CreateQualifiedName(string unqualifiedName)
            {
                return Name + "." + unqualifiedName;
            }
        }

        public object IPerformaceCounterWriter { get; set; }
    }
}