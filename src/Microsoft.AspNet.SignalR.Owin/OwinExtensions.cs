// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.AspNet.SignalR.Owin.Handlers;

namespace Owin
{
    public static class OwinExtensions
    {
        public static IAppBuilder MapHubs(this IAppBuilder builder)
        {
            return builder.UseType<HubDispatcherHandler>(GlobalHost.DependencyResolver);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path)
        {
            return builder.UseType<HubDispatcherHandler>(path, GlobalHost.DependencyResolver);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, IDependencyResolver resolver)
        {
            return builder.UseType<HubDispatcherHandler>(resolver);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path, IDependencyResolver resolver)
        {
            return builder.UseType<HubDispatcherHandler>(path, resolver);
        }

        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url)
        {
            return builder.UseType<PersistentConnectionHandler>(url, typeof(T), GlobalHost.DependencyResolver);
        }

        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url, IDependencyResolver resolver)
        {
            return builder.UseType<PersistentConnectionHandler>(url, typeof(T), resolver);
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType)
        {
            return builder.UseType<PersistentConnectionHandler>(url, connectionType, GlobalHost.DependencyResolver);
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType, IDependencyResolver resolver)
        {
            return builder.UseType<PersistentConnectionHandler>(url, connectionType, resolver);
        }

        private static IAppBuilder UseType<T>(this IAppBuilder builder, params object[] args)
        {
            if (args.Length > 0)
            {
                var resolver = args[args.Length - 1] as IDependencyResolver;
                if (resolver != null)
                {
                    var env = builder.Properties;
                    CancellationToken token = env.GetShutdownToken();
                    string instanceName = env.GetAppInstanceName();

                    resolver.InitializeHost(instanceName, token);
                }
            }

            return builder.Use(typeof(T), args);
        }
    }
}
