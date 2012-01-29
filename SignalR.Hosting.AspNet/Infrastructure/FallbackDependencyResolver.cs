using System;
using System.Collections.Generic;
using System.Linq;
using SignalR.Infrastructure;

namespace SignalR.Hosting.AspNet.Infrastructure
{
    public class FallbackDependencyResolver : IDependencyResolver
    {
        private readonly IDependencyResolver _resolver;
        private readonly IDependencyResolver _fallbackResolver;

        public FallbackDependencyResolver(IDependencyResolver resolver, IDependencyResolver fallbackResolver)
        {
            _resolver = resolver;
            _fallbackResolver = fallbackResolver;
        }

        public object GetService(Type serviceType)
        {
            return _resolver.GetService(serviceType) ?? _fallbackResolver.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _resolver.GetServices(serviceType).Concat(_fallbackResolver.GetServices(serviceType));
        }

        public void Register(Type serviceType, Func<object> activator)
        {
            _resolver.Register(serviceType, activator);
        }

        public void Register(Type serviceType, IEnumerable<Func<object>> activators)
        {
            _resolver.Register(serviceType, activators);
        }
    }

}
