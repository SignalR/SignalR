// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.SystemWeb.Infrastructure;
using Microsoft.Owin.Host.SystemWeb;
using Owin;

namespace System.Web.Routing
{
    public static class SignalRRouteExtensions
    {
        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <typeparam name="T">The type of <see cref="PersistentConnection"/>.</typeparam>
        /// <param name="name">The name of the route.</param>
        /// <param name="url">path of the route.</param>
        /// <returns>The registered route.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url) where T : PersistentConnection
        {
            return MapConnection(routes, name, url, typeof(T), new ConnectionConfiguration());
        }

        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <typeparam name="T">The type of <see cref="PersistentConnection"/>.</typeparam>
        /// <param name="name">The name of the route.</param>
        /// <param name="url">path of the route.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <returns>The registered route.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url, ConnectionConfiguration configuration) where T : PersistentConnection
        {
            return MapConnection(routes, name, url, typeof(T), configuration);
        }

        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <typeparam name="T">The type of <see cref="PersistentConnection"/>.</typeparam>
        /// <param name="name">The name of the route.</param>
        /// <param name="url">path of the route.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <param name="build">An action to further configure the OWIN pipeline.</param>
        /// <returns>The registered route</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url, ConnectionConfiguration configuration, Action<IAppBuilder> build) where T : PersistentConnection
        {
            return MapConnection(routes, name, url, typeof(T), configuration, build);
        }

        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="url">path of the route.</param>
        /// <param name="type">The type of <see cref="PersistentConnection"/>.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type, ConnectionConfiguration configuration)
        {
            InitializeProtectedData(configuration);

            return routes.MapOwinPath(name, url, map => map.MapConnection(String.Empty, type, configuration));
        }

        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="url">path of the route.</param>
        /// <param name="type">The type of <see cref="PersistentConnection"/>.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <param name="build">An action to further configure the OWIN pipeline.</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type, ConnectionConfiguration configuration, Action<IAppBuilder> build)
        {
            InitializeProtectedData(configuration);

            return routes.MapOwinPath(name, url, map =>
            {
                build(map);
                map.MapConnection(String.Empty, type, configuration);
            });
        }

        /// <summary>
        /// Initializes the default hub route (/signalr).
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <returns>The registered route.</returns>
        public static RouteBase MapHubs(this RouteCollection routes)
        {
            return routes.MapHubs(new HubConfiguration());
        }

        /// <summary>
        /// Initializes the default hub route (/signalr).
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <returns>The registered route.</returns>
        public static RouteBase MapHubs(this RouteCollection routes, HubConfiguration configuration)
        {
            return routes.MapHubs("/signalr", configuration);
        }

        /// <summary>
        /// Initializes the hub route using specified configuration.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="path">The path of the hubs route.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <returns>The registered route.</returns>
        public static RouteBase MapHubs(this RouteCollection routes, string path, HubConfiguration configuration)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return routes.MapHubs("signalr.hubs", path, configuration, _ => { });
        }

        /// <summary>
        /// Initializes the hub route using specified configuration.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="path">The path of the hubs route.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <param name="build">An action to further configure the OWIN pipeline.</param>
        /// <returns>The registered route.</returns>
        public static RouteBase MapHubs(this RouteCollection routes, string path, HubConfiguration configuration, Action<IAppBuilder> build)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return routes.MapHubs("signalr.hubs", path, configuration, build);
        }

        /// <summary>
        /// Initializes the hub route using specified configuration.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="path">The path of the hubs route.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <param name="build"></param>
        /// <returns>The registered route.</returns>
        internal static RouteBase MapHubs(this RouteCollection routes, string name, string path, HubConfiguration configuration, Action<IAppBuilder> build)
        {
            var locator = new Lazy<IAssemblyLocator>(() => new BuildManagerAssemblyLocator());
            configuration.Resolver.Register(typeof(IAssemblyLocator), () => locator.Value);

            InitializeProtectedData(configuration);

            return routes.MapOwinPath(name, path, map =>
            {
                build(map);
                map.MapHubs(String.Empty, configuration);
            });
        }

        private static void InitializeProtectedData(ConnectionConfiguration configuration)
        {
            var protectedData = new MachineKeyProtectedData();
            configuration.Resolver.Register(typeof(IProtectedData), () => protectedData);
        }
    }
}
