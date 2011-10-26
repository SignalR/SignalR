using System;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class DefaultHubActivator : IHubActivator
    {
        public IHub Create(Type hubType)
        {
            object hub = DependencyResolver.Resolve(hubType) ?? Activator.CreateInstance(hubType);
            return hub as IHub;
        }
    }
}