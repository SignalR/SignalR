// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Owin.Middleware;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security.DataProtection;

namespace Owin
{
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Owin", Justification = "The owin namespace is for consistentcy.")]
    public static class OwinExtensions
    {
        public static IAppBuilder MapHubs(this IAppBuilder builder)
        {
            return builder.MapHubs(new HubConfiguration());
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, HubConfiguration configuration)
        {
            return builder.MapHubs("/signalr", configuration);
        }

        public static IAppBuilder MapHubs(this IAppBuilder builder, string path, HubConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return builder.UseType<HubDispatcherMiddleware>(path, configuration);
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url) where T : PersistentConnection
        {
            return builder.MapConnection(url, typeof(T), new ConnectionConfiguration());
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string url, ConnectionConfiguration configuration) where T : PersistentConnection
        {
            return builder.MapConnection(url, typeof(T), configuration);
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string url, Type connectionType, ConnectionConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return builder.UseType<PersistentConnectionMiddleware>(url, connectionType, configuration);
        }

        private static IAppBuilder UseType<T>(this IAppBuilder builder, params object[] args)
        {
            ConnectionConfiguration configuration = null;

            // Ensure we have the conversions for MS.Owin so that
            // the app builder respects the OwinMiddleware base class
            SignatureConversions.AddConversions(builder);

            if (args.Length > 0)
            {
                configuration = args[args.Length - 1] as ConnectionConfiguration;

                if (configuration == null)
                {
                    throw new ArgumentException(Resources.Error_NoConfiguration);
                }

                var resolver = configuration.Resolver;

                if (resolver == null)
                {
                    throw new ArgumentException(Resources.Error_NoDepenendeyResolver);
                }

                var env = builder.Properties;
                CancellationToken token = env.GetShutdownToken();

                // If we don't get a valid instance name then generate a random one
                string instanceName = env.GetAppInstanceName() ?? Guid.NewGuid().ToString();

                // Use the data protection provider from app builder and fallback to the
                // Dpapi provider
                IDataProtectionProvider provider = builder.GetDataProtectionProvider();
                IProtectedData protectedData;

                // If we're using DPAPI then fallback to the default protected data if running
                // on mono since it doesn't support any of this
                if (provider == null && MonoUtility.IsRunningMono)
                {
                    protectedData = new DefaultProtectedData();
                }
                else
                {
                    if (provider == null)
                    {
                        provider = new DpapiDataProtectionProvider(instanceName);
                    }

                    protectedData = new DataProtectionProviderProtectedData(provider);
                }

                resolver.Register(typeof(IProtectedData), () => protectedData);

                // Try to get the list of reference assemblies from the host
                IEnumerable<Assembly> referenceAssemblies = env.GetReferenceAssemblies();
                if (referenceAssemblies != null)
                {
                    // Use this list as the assembly locator
                    var assemblyLocator = new EnumerableOfAssemblyLocator(referenceAssemblies);
                    resolver.Register(typeof(IAssemblyLocator), () => assemblyLocator);
                }

                resolver.InitializeHost(instanceName, token);
            }

            builder.Use(typeof(T), args);

            return builder;
        }
    }
}
