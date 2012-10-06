using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class AttributeAuthModule : HubPipelineModule
    {
        private readonly IEnumerable<IAuthorizeHubConnection> _defaultConnectionAuthorizers;
        private readonly IEnumerable<IAuthorizeHubMethodInvocation> _defaultInvocationAuthorizers;
        private readonly ConcurrentDictionary<Type, IEnumerable<IAuthorizeHubConnection>> _connectionAuthorizers;
        private readonly ConcurrentDictionary<Type, IEnumerable<IAuthorizeHubMethodInvocation>> _classInvocationAuthorizers;
        private readonly ConcurrentDictionary<MethodDescriptor, IEnumerable<IAuthorizeHubMethodInvocation>> _methodInvocationAuthorizers;

        public AttributeAuthModule()
            : this(null, null)
        {
        }

        public AttributeAuthModule(AuthorizeAttribute authAttribute)
            : this(new[] { authAttribute }, new[] { authAttribute })
        {
        }

        public AttributeAuthModule(IEnumerable<IAuthorizeHubConnection> connectionAuthorizers, IEnumerable<IAuthorizeHubMethodInvocation> invocationAuthorizes)
        {
            _defaultConnectionAuthorizers = connectionAuthorizers;
            _defaultInvocationAuthorizers = invocationAuthorizes;
            _connectionAuthorizers = new ConcurrentDictionary<Type, IEnumerable<IAuthorizeHubConnection>>();
            _classInvocationAuthorizers = new ConcurrentDictionary<Type, IEnumerable<IAuthorizeHubMethodInvocation>>();
            _methodInvocationAuthorizers = new ConcurrentDictionary<MethodDescriptor, IEnumerable<IAuthorizeHubMethodInvocation>>();
        }

        public override Func<IHub, bool> BuildAuthorizeConnect(Func<IHub, bool> authorizeConnect)
        {
            return base.BuildAuthorizeConnect(hub =>
            {
                // Call HubDispatcher.AuthorizedConnection and other custom modules first
                if (!authorizeConnect(hub))
                {
                    return false;
                }

                if (_defaultConnectionAuthorizers != null && !_defaultConnectionAuthorizers.All(a => a.AuthorizeHubConnection(hub)))
                {
                    return false;
                }

                var authorizers = _connectionAuthorizers.GetOrAdd(hub.GetType(),
                    hubType => hubType.GetCustomAttributes(typeof(IAuthorizeHubConnection), inherit: true).Cast<IAuthorizeHubConnection>());
                return authorizers.All(a => a.AuthorizeHubConnection(hub));
            });
        }

        public override Func<IHubIncomingInvokerContext, Task<object>> BuildIncoming(Func<IHubIncomingInvokerContext, Task<object>> invoke)
        {
            return base.BuildIncoming(context =>
            {
                if (_defaultInvocationAuthorizers == null || _defaultInvocationAuthorizers.All(a => a.AuthorizeHubMethodInvocation(context)))
                {
                    var classLevelAuthorizers = _classInvocationAuthorizers.GetOrAdd(context.Hub.GetType(),
                        hubType => hubType.GetCustomAttributes(typeof(IAuthorizeHubMethodInvocation), inherit: true).Cast<IAuthorizeHubMethodInvocation>());

                    if (classLevelAuthorizers.All(a => a.AuthorizeHubMethodInvocation(context)))
                    {
                        var methodLevelAuthorizers = _methodInvocationAuthorizers.GetOrAdd(context.MethodDescriptor,
                            methodDescriptor => methodDescriptor.Attributes.OfType<IAuthorizeHubMethodInvocation>());

                        if (methodLevelAuthorizers.All(a => a.AuthorizeHubMethodInvocation(context)))
                        {
                            return invoke(context);
                        }
                    }
                }
                
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(new NotAuthorizedException(String.Format("Caller is not authorized to invoke the {0} method on {1}.",
                                                                          context.MethodDescriptor.Name,
                                                                          context.MethodDescriptor.Hub.Name)));
                return tcs.Task;
            });
        }
    }
}
