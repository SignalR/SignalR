using System;

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
    }
}
