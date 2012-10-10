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
        private readonly List<HubDescriptor> _hubs = new List<HubDescriptor>();
        private readonly string _url;

        private IJavaScriptProxyGenerator _proxyGenerator;
        private IHubManager _manager;
        private IHubRequestParser _requestParser;
        private IParameterResolver _binder;
        private IHubPipelineInvoker _pipelineInvoker;
        private IPerformanceCounterManager _counters;
        private bool _isDebuggingEnabled;

        private static readonly MethodInfo _continueWithMethod = typeof(HubDispatcher).GetMethod("ContinueWith", BindingFlags.NonPublic | BindingFlags.Static);

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

        public override void Initialize(IDependencyResolver resolver, HostContext context)
        {
            _proxyGenerator = resolver.Resolve<IJavaScriptProxyGenerator>();
            _manager = resolver.Resolve<IHubManager>();
            _binder = resolver.Resolve<IParameterResolver>();
            _requestParser = resolver.Resolve<IHubRequestParser>();
            _pipelineInvoker = resolver.Resolve<IHubPipelineInvoker>();

            _counters = resolver.Resolve<IPerformanceCounterManager>();

            // Call base initializer before populating _hubs so the _jsonSerializer is initialized
            base.Initialize(resolver, context);

            // Populate _hubs
            string data = context.Request.QueryStringOrForm("connectionData");

            if (!String.IsNullOrEmpty(data))
            {
                var clientHubInfo = _jsonSerializer.Parse<IEnumerable<ClientHubInfo>>(data);
                if (clientHubInfo != null)
                {
                    foreach (var hubInfo in clientHubInfo)
                    {
                        // Try to find the associated hub type
                        HubDescriptor hubDescriptor = _manager.EnsureHub(hubInfo.Name,
                            _counters.ErrorsHubResolutionTotal,
                            _counters.ErrorsHubResolutionPerSec,
                            _counters.ErrorsAllTotal,
                            _counters.ErrorsAllPerSec);

                        if (_pipelineInvoker.AuthorizeConnect(hubDescriptor, context.Request))
                        {
                            // Add this to the list of hub descriptors this connection is interested in
                            _hubs.Add(hubDescriptor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes the hub's incoming method calls.
        /// </summary>
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            HubRequest hubRequest = _requestParser.Parse(data);

            // Create the hub
            HubDescriptor descriptor = _manager.EnsureHub(hubRequest.Hub,
                _counters.ErrorsHubInvocationTotal,
                _counters.ErrorsHubInvocationPerSec,
                _counters.ErrorsAllTotal,
                _counters.ErrorsAllPerSec);

            IJsonValue[] parameterValues = hubRequest.ParameterValues;

            // Resolve the method
            MethodDescriptor methodDescriptor = _manager.GetHubMethod(descriptor.Name, hubRequest.Method, parameterValues);
            if (methodDescriptor == null)
            {
                _counters.ErrorsHubInvocationTotal.Increment();
                _counters.ErrorsHubInvocationPerSec.Increment();
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
            return hub.OnConnected();
        }

        internal static Task Reconnect(IHub hub)
        {
            return hub.OnReconnected();
        }

        internal static Task Disconnect(IHub hub)
        {
            return hub.OnDisconnected();
        }

        internal static IEnumerable<string> RejoiningGroups(IHub hub, IEnumerable<string> groups)
        {
            return hub.RejoiningGroups(groups);
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
                        MethodInfo continueWithMethod = _continueWithMethod.MakeGenericMethod(resultType);

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

        internal static Task Outgoing(IHubOutgoingInvokerContext context)
        {
            var message = new ConnectionMessage(context.Signal, context.Invocation)
            {
                ExcludedSignals = context.ExcludedSignals
            };

            return context.Connection.Send(message);
        }

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return ExecuteHubEventAsync(request, connectionId, hub => _pipelineInvoker.Connect(hub));
        }

        protected override Task OnReconnectedAsync(IRequest request, string connectionId)
        {
            return ExecuteHubEventAsync(request, connectionId, hub => _pipelineInvoker.Reconnect(hub));
        }

        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return GetHubs(request, connectionId)
                   .Select(hub =>
                   {
                       string groupPrefix = hub.GetType().Name + ".";
                       IEnumerable<string> groupsToRejoin = _pipelineInvoker.RejoiningGroups(hub, groups.Where(g => g.StartsWith(groupPrefix))
                                                                                                         .Select(g => g.Substring(groupPrefix.Length)))
                                                                             .Select(g => groupPrefix + g).ToList();
                       hub.Dispose();
                       return groupsToRejoin;
                   })
                   .SelectMany(groupsToRejoin => groupsToRejoin);
        }

        protected override Task OnDisconnectAsync(IRequest request, string connectionId)
        {
            return ExecuteHubEventAsync(request, connectionId, hub => _pipelineInvoker.Disconnect(hub));
        }

        protected override IEnumerable<string> GetSignals(string connectionId)
        {
            return _hubs.SelectMany(info => new[] { info.Name, info.CreateQualifiedName(connectionId) })
                        .Concat(base.GetSignals(connectionId));
        }

        private Task ExecuteHubEventAsync(IRequest request, string connectionId, Func<IHub, Task> action)
        {
            var hubs = GetHubs(request, connectionId).ToList();
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

                    hub.Context = new HubCallerContext(request, connectionId);
                    hub.Clients = new HubConnectionContext(_pipelineInvoker, Connection, descriptor.Name, connectionId, state);
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

        private IEnumerable<IHub> GetHubs(IRequest request, string connectionId)
        {
            return from descriptor in _hubs
                   select CreateHub(request, descriptor, connectionId) into hub
                   where hub != null
                   select hub;
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
                _counters.ErrorsHubInvocationTotal.Increment();
                _counters.ErrorsHubInvocationPerSec.Increment();
                _counters.ErrorsAllTotal.Increment();
                _counters.ErrorsAllPerSec.Increment();
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

        private static void ContinueWith<T>(Task<T> task, TaskCompletionSource<object> tcs)
        {
            if (task.IsCompleted)
            {
                // Fast path for tasks that completed synchronously
                ContinueSync<T>(task, tcs);
            }
            else
            {
                ContinueAsync<T>(task, tcs);
            }
        }

        private static void ContinueSync<T>(Task<T> task, TaskCompletionSource<object> tcs)
        {
            if (task.IsFaulted)
            {
                tcs.TrySetException(task.Exception);
            }
            else if (task.IsCanceled)
            {
                tcs.TrySetCanceled();
            }
            else
            {
                tcs.TrySetResult(task.Result);
            }
        }

        private static void ContinueAsync<T>(Task<T> task, TaskCompletionSource<object> tcs)
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

        private class ClientHubInfo
        {
            public string Name { get; set; }
        }
    }
}