﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalR.Hosting;

namespace SignalR.Hubs
{
    public class HubDispatcher : PersistentConnection
    {
        private IJavaScriptProxyGenerator _proxyGenerator;
        private IHubManager _manager;
        private IParameterResolver _binder;
        private HostContext _context;

        private readonly string _url;

        public HubDispatcher(string url)
        {
            _url = url;
        }

        public override void Initialize(IDependencyResolver resolver)
        {
            _proxyGenerator = resolver.Resolve<IJavaScriptProxyGenerator>();
            _manager = resolver.Resolve<IHubManager>();
            _binder = resolver.Resolve<IParameterResolver>();

            base.Initialize(resolver);
        }

        protected override Task OnReceivedAsync(string connectionId, string data)
        {
            var hubRequest = HubRequest.Parse(data);

            // Create the hub
            HubDescriptor descriptor = _manager.EnsureHub(hubRequest.Hub);

            JToken[] parameterValues = hubRequest.ParameterValues;

            // Resolve the action
            MethodDescriptor actionDescriptor = _manager.GetHubMethod(descriptor.Name, hubRequest.Action, parameterValues);
            if (actionDescriptor == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' action could not be resolved.", hubRequest.Action));
            }

            // Resolving the actual state object
            var state = new TrackingDictionary(hubRequest.State);
            var hub = CreateHub(descriptor, connectionId, state);
            Task resultTask;

            try
            {
                // Invoke the action
                object result = actionDescriptor.Invoker.Invoke(hub, _binder.ResolveMethodParameters(actionDescriptor, parameterValues));
                Type returnType = result != null ? result.GetType() : actionDescriptor.ReturnType;

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
            var operations = GetHubsImplementingInterface(typeof(T))
                .Select(hub => CreateHub(hub, connectionId))
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
            var hub = _manager.ResolveHub(descriptor.Name);

            if (hub != null)
            {
                state = state ?? new TrackingDictionary();
                hub.Context = new HubContext(_context, connectionId);
                hub.Caller = new SignalAgent(Connection, connectionId, descriptor.Name, state);
                var agent = new ClientAgent(Connection, descriptor.Name);
                hub.Agent = agent;
                hub.GroupManager = agent;
            }

            return hub;
        }

        private IEnumerable<HubDescriptor> GetHubsImplementingInterface(Type interfaceType)
        {
            // Get hubs that implement the specified interface
            return _manager.GetHubs(hub => interfaceType.IsAssignableFrom(hub.Type));
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
            var hub = _manager.EnsureHub(hubInfo.Name);

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
            private static readonly JToken[] _emptyArgs = new JToken[0];

            public static HubRequest Parse(string data)
            {
                var rawRequest = JObject.Parse(data);
                var request = new HubRequest();

                // TODO: Figure out case insensitivity in JObject.Parse, this should cover our clients for now
                request.Hub = rawRequest.Value<string>("hub") ?? rawRequest.Value<string>("Hub");
                request.Action = rawRequest.Value<string>("action") ?? rawRequest.Value<string>("Action");
                request.Id = rawRequest.Value<string>("id") ?? rawRequest.Value<string>("Id");

                var rawState = rawRequest["state"] ?? rawRequest["State"];
                request.State = rawState == null ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) :
                                           rawState.ToObject<IDictionary<string, object>>();

                var rawArgs = rawRequest["data"] ?? rawRequest["Data"];
                request.ParameterValues = rawArgs == null ? _emptyArgs :
                                                    rawArgs.Children().ToArray();

                return request;
            }

            public string Hub { get; set; }
            public string Action { get; set; }
            public JToken[] ParameterValues { get; set; }
            public IDictionary<string, object> State { get; set; }
            public string Id { get; set; }
        }
    }
}