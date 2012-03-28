using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalR
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

        public static IEnumerable<T> ResolveAll<T>(this IDependencyResolver resolver)
        {
            return resolver.GetServices(typeof(T)).Cast<T>();
        }

        public static IEnumerable<object> ResolveAll(this IDependencyResolver resolver, Type type)
        {
            return resolver.GetServices(type);
        }
    }
}
