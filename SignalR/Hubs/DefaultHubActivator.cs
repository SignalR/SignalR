using System;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class DefaultHubActivator : IHubActivator
    {
        private readonly IDependencyResolver _resolver;

        public DefaultHubActivator(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public IHub Create(Type hubType)
        {
            object hub = _resolver.Resolve(hubType) ?? Activator.CreateInstance(hubType);
            return hub as IHub;
        }
    }
}