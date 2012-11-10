// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.SystemWeb.Infrastructure;
using Microsoft.Owin.Host.SystemWeb;
using Owin;

namespace Microsoft.AspNet.SignalR
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
            return MapConnection(routes, name, url, typeof(T), GlobalHost.DependencyResolver);
        }

        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <typeparam name="T">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="name">The name of the route</param>
        /// <param name="url">path pattern of the route. Should end with catch-all parameter.</param>
        /// <param name="resolver">The dependency resolver to use for this connection</param>
        /// <returns>The registered route</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url, IDependencyResolver resolver) where T : PersistentConnection
        {
            return MapConnection(routes, name, url, typeof(T), resolver);
        }

        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <param name="type">The type of <see cref="PersistentConnection"/></param>
        /// <param name="name">The name of the route</param>
        /// <param name="url">path pattern of the route. Should end with catch-all parameter.</param>
        /// <returns>The registered route</returns>
        /// <example>
        /// routes.MapConnection("echo", "echo/{*operation}", typeof(MyConnection));
        /// </example>
        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type)
        {
            return MapConnection(routes, name, url, type, GlobalHost.DependencyResolver);
        }

        /// <summary>
        /// Initializes the default hub route (~/signalr).
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapHubs(this RouteCollection routes)
        {
            return MapHubs(routes, GlobalHost.DependencyResolver);
        }

        /// <summary>
        /// Changes the dependency resolver for the default hub route (~/signalr).
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <param name="resolver">The dependency resolver to use for the hub connection</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapHubs(this RouteCollection routes, IDependencyResolver resolver)
        {
            return MapHubs(routes, "~/signalr", resolver);
        }

        /// <summary>
        /// Changes the default hub route from ~/signalr to a specified path.
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <param name="url">The path of the hubs route. This should *NOT* contain catch-all parameter.</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapHubs(this RouteCollection routes, string url)
        {
            return MapHubs(routes, url, GlobalHost.DependencyResolver);
        }

        /// <summary>
        /// Changes the default hub route from ~/signalr to a specified path.
        /// </summary>
        /// <param name="routes">The route table</param>
        /// <param name="url">The path of the hubs route. This should *NOT* contain catch-all parameter.</param>
        /// <param name="resolver">The dependency resolver to use for the hub connection</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapHubs(this RouteCollection routes, string url, IDependencyResolver resolver)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            var existing = routes["signalr.hubs"];
            if (existing != null)
            {
                routes.Remove(existing);
            }

            var routeUrl = url.TrimStart('~').TrimStart('/');

            var locator = new Lazy<IAssemblyLocator>(() => new BuildManagerAssemblyLocator());
            resolver.Register(typeof(IAssemblyLocator), () => locator.Value);

            return routes.MapOwinRoute("signalr.hubs", routeUrl, map => map.MapHubs(resolver));
        }


        /// <summary>
        /// Maps a <see cref="PersistentConnection"/> with the default dependency resolver to the specified path.
        /// </summary>        
        /// <param name="routes">The route table</param>
        /// <param name="name">The name of the route</param>
        /// <param name="url">path pattern of the route. Should end with catch-all parameter.</param>
        /// <param name="type">The type of <see cref="PersistentConnection"/></param>
        /// <param name="resolver">The dependency resolver to use for the hub connection</param>
        /// <returns>The registered route</returns>
        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type, IDependencyResolver resolver)
        {
            var routeUrl = url ?? String.Empty;
            var catchAllIndex = routeUrl.LastIndexOf("/{*", StringComparison.Ordinal);
            if (routeUrl.EndsWith("}", StringComparison.OrdinalIgnoreCase) && catchAllIndex != -1)
            {
                routeUrl = routeUrl.Substring(0, catchAllIndex);
            }
            routeUrl = routeUrl.TrimStart('~').TrimStart('/');

            return routes.MapOwinRoute(name, routeUrl, map => map.MapConnection(String.Empty, type, resolver));
        }
    }
}
