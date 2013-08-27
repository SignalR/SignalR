﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR;
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
        [Obsolete("Use IAppBuilder.MapSignalR<TConnection> in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url) where T : PersistentConnection
        {
            throw new NotImplementedException();
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
        [Obsolete("Use IAppBuilder.MapSignalR<TConnection> in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url, ConnectionConfiguration configuration) where T : PersistentConnection
        {
            throw new NotImplementedException();
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
        [Obsolete("Use IAppBuilder.MapSignalR<TConnection> in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url, ConnectionConfiguration configuration, Action<IAppBuilder> build) where T : PersistentConnection
        {
            throw new NotImplementedException();
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
        [Obsolete("Use IAppBuilder.MapSignalR<TConnection> in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type, ConnectionConfiguration configuration)
        {
            throw new NotImplementedException();
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
        /// <returns>The registered route</returns>.
        [Obsolete("Use IAppBuilder.MapSignalR<TConnection> in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type, ConnectionConfiguration configuration, Action<IAppBuilder> build)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes the default hub route (/signalr).
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <returns>The registered route.</returns>
        [Obsolete("Use IAppBuilder.MapSignalR in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapHubs(this RouteCollection routes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes the default hub route (/signalr).
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <returns>The registered route.</returns>
        [Obsolete("Use IAppBuilder.MapSignalR in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapHubs(this RouteCollection routes, HubConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes the hub route using specified configuration.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="path">The path of the hubs route.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <returns>The registered route.</returns>
        [Obsolete("Use IAppBuilder.MapSignalR in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapHubs(this RouteCollection routes, string path, HubConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes the hub route using specified configuration.
        /// </summary>
        /// <param name="routes">The route table.</param>
        /// <param name="path">The path of the hubs route.</param>
        /// <param name="configuration">Configuration options.</param>
        /// <param name="build">An action to further configure the OWIN pipeline.</param>
        /// <returns>The registered route.</returns>
        [Obsolete("Use IAppBuilder.MapSignalR in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        public static RouteBase MapHubs(this RouteCollection routes, string path, HubConfiguration configuration, Action<IAppBuilder> build)
        {
            throw new NotImplementedException();
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
        [Obsolete("Use IAppBuilder.MapSignalR in an Owin Startup class. See http://go.microsoft.com/fwlink/?LinkId=320578 for more details.", true)]
        internal static RouteBase MapHubs(this RouteCollection routes, string name, string path, HubConfiguration configuration, Action<IAppBuilder> build)
        {
            throw new NotImplementedException();
        }
    }
}
