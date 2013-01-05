// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Owin;
using Microsoft.AspNet.SignalR.Owin.Handlers;

namespace Owin
{
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Owin", Justification = "The owin namespace is for consistentcy.")]
    public static class OwinExtensions
    {
        public static IAppBuilder MapHubs(this IAppBuilder builder)
        {
            return builder.MapHubs(String.Empty);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path)
        {
            return builder.UseType<HubDispatcherHandler>(path, new HubConfiguration());
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path, HubConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return builder.UseType<HubDispatcherHandler>(path, configuration.EnableJavaScriptProxies, configuration.Resolver);
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url) where T : PersistentConnection
        {
            return builder.MapConnection(url, typeof(T), new ConnectionConfiguration());
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType, ConnectionConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return builder.UseType<PersistentConnectionHandler>(url, connectionType, configuration.Resolver);
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
