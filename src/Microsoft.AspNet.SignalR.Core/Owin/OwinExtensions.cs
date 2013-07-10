// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Owin.Middleware;
using Microsoft.AspNet.SignalR.Tracing;
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

            return builder.Map(path, subApp => subApp.UseHubs(configuration));
        }

        public static IAppBuilder UseHubs(this IAppBuilder builder, HubConfiguration configuration)
        {
            return builder.UseSignalR<HubDispatcherMiddleware>(configuration);
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder MapConnection<TConnection>(this IAppBuilder builder, string path) where TConnection : PersistentConnection
        {
            return builder.MapConnection(path, typeof(TConnection), new ConnectionConfiguration());
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder MapConnection<TConnection>(this IAppBuilder builder, string path, ConnectionConfiguration configuration) where TConnection : PersistentConnection
        {
            return builder.MapConnection(path, typeof(TConnection), configuration);
        }

        public static IAppBuilder MapConnection(this IAppBuilder builder, string path, Type connectionType, ConnectionConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return builder.Map(path, subApp => subApp.UseConnection(connectionType, configuration));
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder UseConnection<TConnection>(this IAppBuilder builder) where TConnection : PersistentConnection
        {
            return builder.UseConnection<TConnection>(new ConnectionConfiguration());
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder UseConnection<TConnection>(this IAppBuilder builder, ConnectionConfiguration configuration) where TConnection : PersistentConnection
        {
            return builder.UseConnection(typeof(TConnection), configuration);
        }

        public static IAppBuilder UseConnection(this IAppBuilder builder, Type connectionType, ConnectionConfiguration configuration)
        {
            return builder.UseSignalR<PersistentConnectionMiddleware>(connectionType, configuration);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This class wires up new dependencies from the host")]
        private static IAppBuilder UseSignalR<T>(this IAppBuilder builder, params object[] args)
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

                // If the host provides trace output then add a default trace listener
                TextWriter traceOutput = env.GetTraceOutput();
                if (traceOutput != null)
                {
                    var hostTraceListener = new TextWriterTraceListener(traceOutput);
                    var traceManager = new TraceManager(hostTraceListener);
                    resolver.Register(typeof(ITraceManager), () => traceManager);
                }

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
