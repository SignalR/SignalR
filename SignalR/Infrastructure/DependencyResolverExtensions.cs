using System;
using System.Collections.Generic;

namespace SignalR.Infrastructure
{
    public static class DependencyResolverExtensions
    {
        public static T Resolve<T>(this IDependencyResolver resolver)
        {
            return (T)resolver.GetService(typeof(T));
        }

        public static object Resolve(this IDependencyResolver resolver, Type type)
        {
            return resolver.GetService(type);
        }

        public static void Register(this IDependencyResolver resolver, Type type, Func<object> activator)
        {
            resolver.Register(type, activator);
        }

        public static void Register(this IDependencyResolver resolver, Type serviceType, IEnumerable<Func<object>> activators)
        {
            resolver.Register(serviceType, activators);
        }

    }
}
