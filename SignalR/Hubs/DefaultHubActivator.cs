using System;

namespace SignalR.Hubs
{
    using SignalR.Hubs.Lookup.Descriptors;

    public class DefaultHubActivator : IHubActivator
    {
        private readonly IDependencyResolver _resolver;

        public DefaultHubActivator(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public IHub Create(HubDescriptor descriptor)
        {
            object hub = _resolver.Resolve(descriptor.Type) ?? Activator.CreateInstance(descriptor.Type);
            return hub as IHub;
        }
    }
}