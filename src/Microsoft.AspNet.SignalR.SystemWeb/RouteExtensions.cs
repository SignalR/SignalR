// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.SystemWeb.Infrastructure;
using Microsoft.Owin.Host.SystemWeb;
using Owin;

namespace System.Web.Routing
{
    public static class RouteExtensions
    {
        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <typeparam name="T">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="name">The name of the route</param>
        /// <param name="url">path pattern of the route. Should end with catch-all parameter.</param>
        /// <returns>The registered route</returns>
        /// <example>
        /// routes.MapConnection{MyConnection}("echo", "echo/{*operation}");
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url) where T : PersistentConnection
        {
            return MapConnection(routes, name, url, typeof(T), new ConnectionConfiguration());
        }

        /// <summary>
        /// Initializes the default hub route (/signalr).
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapHubs(this RouteCollection routes)
        {
            return MapHubs(routes, "/signalr", new HubConfiguration());
        }

        /// <summary>
        /// Initializes the hub route using specified settings.
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <param name="path">The path of the hubs route. This should *NOT* contain catch-all parameter.</param>
        /// <param name="settings">Configuration options</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapHubs(this RouteCollection routes, string path, HubConfiguration settings)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            var locator = new Lazy<IAssemblyLocator>(() => new BuildManagerAssemblyLocator());
            settings.Resolver.Register(typeof(IAssemblyLocator), () => locator.Value);

            return routes.MapOwinRoute("signalr.hubs", path, map => map.MapHubs(String.Empty, settings));
        }


        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>        
        /// <param name="routes">The route table</param>
        /// <param name="name">The name of the route</param>
        /// <param name="url">path pattern of the route. Should end with catch-all parameter.</param>
        /// <param name="type">The type of <see cref="PersistentConnection"/></param>
        /// <param name="settings">Configuration options</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type, ConnectionConfiguration settings)
        {
            return routes.MapOwinRoute(name, url, map => map.MapConnection(String.Empty, type, settings));
        }
    }
}
