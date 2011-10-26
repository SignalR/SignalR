using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
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

        private HttpCookieCollection _cookies;
        private IPrincipal _user;

        public HubDispatcher(string url)
            : this(DependencyResolver.Resolve<IHubFactory>(),
                   DependencyResolver.Resolve<IMessageStore>(),
                   Signaler.Instance,
                   DependencyResolver.Resolve<IClientIdFactory>(),
                   DependencyResolver.Resolve<IActionResolver>(),
                   DependencyResolver.Resolve<IJavaScriptProxyGenerator>(),
                   DependencyResolver.Resolve<IJsonStringifier>(),
                   DependencyResolver.Resolve<IHubLocator>(),
                   url)
        {
        }

        public HubDispatcher(IHubFactory hubFactory,
                             IMessageStore store,
                             Signaler signaler,
                             IClientIdFactory clientIdFactory,
                             IActionResolver actionResolver,
                             IJavaScriptProxyGenerator proxyGenerator,
                             IJsonStringifier jsonStringifier,
                             IHubLocator hubLocator,
                             string url)
            : base(signaler, clientIdFactory, store, jsonStringifier)
        {
            _hubFactory = hubFactory;
            _store = store;
            _signaler = signaler;
            _actionResolver = actionResolver;
            _proxyGenerator = proxyGenerator;
            _hubLocator = hubLocator;
            _url = VirtualPathUtility.ToAbsolute(url);
        }

        protected override Task OnReceivedAsync(string clientId, string data)
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
            hub.Context = new HubContext(clientId, _cookies, _user);
            hub.Caller = new SignalAgent(Connection, clientId, hubName, state);
            var agent = new ClientAgent(Connection, hubName);
            hub.Agent = agent;
            hub.GroupManager = agent;

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
                        return task.ContinueWith(t => ProcessResult(state, null, hubRequest, t.Exception));
                    }
                    else
                    {
                        // Get the <T> in Task<T>
                        Type resultType = returnType.GetGenericArguments().Single();

                        // Get the correct ContinueWith overload
                        var continueWith = (from m in task.GetType().GetMethods()
                                            let methodParameters = m.GetParameters()
                                            where m.Name.Equals("ContinueWith", StringComparison.OrdinalIgnoreCase) &&
                                                  methodParameters.Length == 1
                                            let parameter = methodParameters[0]
                                            where parameter.ParameterType.IsGenericType &&
                                                  typeof(Action<>) == parameter.ParameterType.GetGenericTypeDefinition()
                                            select new
                                            {
                                                Method = m,
                                                ArgType = parameter.ParameterType.GetGenericArguments()[0]
                                            })
                                            .FirstOrDefault();

                        var taskParameter = Expression.Parameter(continueWith.ArgType);
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

                        var call = Expression.Call(Expression.Constant(task, continueWith.ArgType), continueWith.Method, lambda);
                        return Expression.Lambda<Func<Task>>(call).Compile()();
                    }
                }
                else
                {
                    ProcessResult(state, result, hubRequest, null);
                }
            }
            catch (TargetInvocationException e)
            {
                ProcessResult(state, null, hubRequest, e);
            }

            return base.OnReceivedAsync(clientId, data);
        }

        public override Task ProcessRequestAsync(HttpContext context)
        {
            // Generate the proxy
            if (context.Request.Path.EndsWith("/hubs", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "application/x-javascript";
                context.Response.Write(_proxyGenerator.GenerateProxy(new HttpContextWrapper(context), _url));
                return TaskAsyncHelper.Empty;
            }

            _cookies = context.Request.Cookies;
            _user = context.User;

            return base.ProcessRequestAsync(context);
        }

        protected override void OnDisconnect(string clientId)
        {
            // Loop over each hub and call disconnect (if the hub supports it)
            foreach (Type type in _hubLocator.GetHubs())
            {
                string hubName = type.FullName;
                IHub hub = _hubFactory.CreateHub(hubName);

                var disconnect = hub as IDisconnect;
                if (disconnect != null)
                {
                    // REVIEW: We don't have any client state here since we're calling this from the server.
                    // Will this match user expectations?
                    var state = new TrackingDictionary();
                    hub.Context = new HubContext(clientId, _cookies, _user);
                    hub.Caller = new SignalAgent(Connection, clientId, hubName, state);
                    var agent = new ClientAgent(Connection, hubName);
                    hub.Agent = agent;
                    hub.GroupManager = agent;

                    disconnect.Disconnect();
                }
            }
        }

        private void ProcessResult(TrackingDictionary state, object result, HubRequest request, Exception error)
        {
            var hubResult = new HubResult
            {
                State = state.GetChanges(),
                Result = result,
                Id = request.Id,
                Error = error != null ? error.GetBaseException().Message : null
            };

            Send(hubResult);
        }

        protected override IConnection CreateConnection(string clientId, IEnumerable<string> groups, HttpContextBase context)
        {
            string data = context.Request["data"];

            if (String.IsNullOrEmpty(data))
            {
                return base.CreateConnection(clientId, groups, context);
            }

            IEnumerable<ClientHubInfo> clientHubInfo = null;

            try
            {
                clientHubInfo = _serializer.Deserialize<IEnumerable<ClientHubInfo>>(data);
            }
            catch
            {

            }

            if (clientHubInfo == null || !clientHubInfo.Any())
            {
                return base.CreateConnection(clientId, groups, context);
            }

            IEnumerable<string> hubSignals = clientHubInfo.SelectMany(info => GetSignals(info, clientId));

            return new Connection(_store, _signaler, null, clientId, hubSignals, groups);
        }

        private IEnumerable<string> GetSignals(ClientHubInfo hubInfo, string clientId)
        {
            var clientSignals = new[] { 
                hubInfo.CreateQualifiedName(clientId),
                hubInfo.CreateQualifiedName(clientId) + "." + PersistentConnection.SignalrCommand
            };

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