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
using Microsoft.Owin.Extensions;

namespace Owin
{
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Owin", Justification = "The owin namespace is for consistentcy.")]
    public static class OwinExtensions
    {
        /// <summary>
        /// Maps SignalR hubs to the app builder pipeline at "/signalr".
        /// </summary>
        /// <param name="builder">The app builder</param>
        public static IAppBuilder MapSignalR(this IAppBuilder builder)
        {
            return builder.MapSignalR(new HubConfiguration());
        }

        /// <summary>
        /// Maps SignalR hubs to the app builder pipeline at "/signalr".
        /// </summary>
        /// <param name="builder">The app builder</param>
        /// <param name="configuration">The <see cref="HubConfiguration"/> to use</param>
        public static IAppBuilder MapSignalR(this IAppBuilder builder, HubConfiguration configuration)
        {
            return builder.MapSignalR("/signalr", configuration);
        }

        /// <summary>
        /// Maps SignalR hubs to the app builder pipeline at the specified path.
        /// </summary>
        /// <param name="builder">The app builder</param>
        /// <param name="path">The path to map signalr hubs</param>
        /// <param name="configuration">The <see cref="HubConfiguration"/> to use</param>
        public static IAppBuilder MapSignalR(this IAppBuilder builder, string path, HubConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return builder.Map(path, subApp => subApp.RunSignalR(configuration));
        }

        /// <summary>
        /// Adds SignalR hubs to the app builder pipeline.
        /// </summary>
        /// <param name="builder">The app builder</param>
        public static void RunSignalR(this IAppBuilder builder)
        {
            builder.RunSignalR(new HubConfiguration());
        }

        /// <summary>
        /// Adds SignalR hubs to the app builder pipeline.
        /// </summary>
        /// <param name="builder">The app builder</param>
        /// <param name="configuration">The <see cref="HubConfiguration"/> to use</param>
        public static void RunSignalR(this IAppBuilder builder, HubConfiguration configuration)
        {
            builder.UseSignalRMiddleware<HubDispatcherMiddleware>(configuration);
        }

        /// <summary>
        /// Maps the specified SignalR <see cref="PersistentConnection"/> to the app builder pipeline at 
        /// the specified path.
        /// </summary>
        /// <typeparam name="TConnection">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="builder">The app builder</param>
        /// <param name="path">The path to map the <see cref="PersistentConnection"/></param>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder MapSignalR<TConnection>(this IAppBuilder builder, string path) where TConnection : PersistentConnection
        {
            return builder.MapSignalR(path, typeof(TConnection), new ConnectionConfiguration());
        }

        /// <summary>
        /// Maps the specified SignalR <see cref="PersistentConnection"/> to the app builder pipeline at 
        /// the specified path.
        /// </summary>
        /// <typeparam name="TConnection">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="builder">The app builder</param>
        /// <param name="path">The path to map the persistent connection</param>
        /// <param name="configuration">The <see cref="ConnectionConfiguration"/> to use</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static IAppBuilder MapSignalR<TConnection>(this IAppBuilder builder, string path, ConnectionConfiguration configuration) where TConnection : PersistentConnection
        {
            return builder.MapSignalR(path, typeof(TConnection), configuration);
        }

        /// <summary>
        /// Maps the specified SignalR <see cref="PersistentConnection"/> to the app builder pipeline at 
        /// the specified path.
        /// </summary>
        /// <param name="builder">The app builder</param>
        /// <param name="path">The path to map the persistent connection</param>
        /// <param name="connectionType">The type of <see cref="PersistentConnection"/></param>
        /// <param name="configuration">The <see cref="ConnectionConfiguration"/> to use</param>
        public static IAppBuilder MapSignalR(this IAppBuilder builder, string path, Type connectionType, ConnectionConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return builder.Map(path, subApp => subApp.RunSignalR(connectionType, configuration));
        }

        /// <summary>
        /// Adds the specified SignalR <see cref="PersistentConnection"/> to the app builder.
        /// </summary>
        /// <typeparam name="TConnection">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="builder">The app builder</param>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static void RunSignalR<TConnection>(this IAppBuilder builder) where TConnection : PersistentConnection
        {
            builder.RunSignalR<TConnection>(new ConnectionConfiguration());
        }

        /// <summary>
        /// Adds the specified SignalR <see cref="PersistentConnection"/> to the app builder.
        /// </summary>
        /// <typeparam name="TConnection">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="builder">The app builder</param>
        /// <param name="configuration">The <see cref="ConnectionConfiguration"/> to use</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static void RunSignalR<TConnection>(this IAppBuilder builder, ConnectionConfiguration configuration) where TConnection : PersistentConnection
        {
            builder.RunSignalR(typeof(TConnection), configuration);
        }

        /// <summary>
        /// Adds the specified SignalR <see cref="PersistentConnection"/> to the app builder.
        /// </summary>
        /// <param name="builder">The app builder</param>
        /// <param name="connectionType">The type of <see cref="PersistentConnection"/></param>
        /// <param name="configuration">The <see cref="ConnectionConfiguration"/> to use</param>
        /// <returns></returns>
        public static void RunSignalR(this IAppBuilder builder, Type connectionType, ConnectionConfiguration configuration)
        {
            builder.UseSignalRMiddleware<PersistentConnectionMiddleware>(connectionType, configuration);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This class wires up new dependencies from the host")]
        private static IAppBuilder UseSignalRMiddleware<T>(this IAppBuilder builder, params object[] args)
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
                    throw new ArgumentException(Resources.Error_NoDependencyResolver);
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

            // BUG 2306: We need to make that SignalR runs before any handlers are
            // mapped in the IIS pipeline so that we avoid side effects like
            // session being enabled. The session behavior can be
            // manually overridden if user calls SetSessionStateBehavior but that shouldn't
            // be a problem most of the time.
            builder.UseStageMarker(PipelineStage.PostAuthorize);

            return builder;
        }
    }
}
