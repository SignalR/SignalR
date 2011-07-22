using System;
using SignalR.Infrastructure;

namespace SignalR.Hubs {
    public class DefaultHubActivator : IHubActivator {
        public Hub Create(Type hubType) {
            object hub = DependencyResolver.Resolve(hubType) ?? Activator.CreateInstance(hubType);
            return hub as Hub;
        }
    }
}