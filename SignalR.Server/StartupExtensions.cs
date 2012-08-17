using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SignalR;
using SignalR.Server;

namespace Owin
{
    public static class StartupExtensions
    {
        public static IAppBuilder MapHubs(this IAppBuilder builder, string path, IDependencyResolver resolver)
        {
            return builder.UseType<HubDispatcherHandler>(path, resolver);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path)
        {
            return builder.UseType<HubDispatcherHandler>(path);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, IDependencyResolver resolver)
        {
            return builder.UseType<HubDispatcherHandler>(resolver);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder)
        {
            return builder.UseType<HubDispatcherHandler>();
        }

        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url, IDependencyResolver resolver)
        {
            return builder.UseType<PersistentConnectionHandler>(url, typeof(T), resolver);
        }

        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url)
        {
            return builder.UseType<PersistentConnectionHandler>(url, typeof(T));
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType, IDependencyResolver resolver)
        {
            return builder.UseType<PersistentConnectionHandler>(url, connectionType, resolver);
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType)
        {
            return builder.UseType<PersistentConnectionHandler>(url, connectionType);
        }
    }
}
