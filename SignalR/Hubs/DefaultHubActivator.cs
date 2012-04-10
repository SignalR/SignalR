using System;

namespace SignalR.Hubs
{
    public class DefaultHubActivator : IHubActivator
    {
        private readonly IDependencyResolver _resolver;

        public DefaultHubActivator(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public IHub Create(HubDescriptor descriptor)
        {
            if(descriptor.Type == null)
            {
                return null;
            }

            object hub = _resolver.Resolve(descriptor.Type) ?? Activator.CreateInstance(descriptor.Type);
            return hub as IHub;
        }
    }
}