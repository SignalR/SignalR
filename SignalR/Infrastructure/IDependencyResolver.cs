using System;
using System.Collections.Generic;

namespace SignalR
{
    public interface IDependencyResolver
    {
        object GetService(Type serviceType);
        IEnumerable<object> GetServices(Type serviceType);
        void Register(Type serviceType, Func<object> activator);
        void Register(Type serviceType, IEnumerable<Func<object>> activators);
    }
}