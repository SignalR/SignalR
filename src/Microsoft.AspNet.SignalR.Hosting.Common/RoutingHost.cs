// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR.Hosting.Common.Routing;

namespace Microsoft.AspNet.SignalR.Hosting.Common
{
    public class RoutingHost : Host
    {
        private readonly RouteManager _routeManager;

        public RoutingHost()
            : this(GlobalHost.DependencyResolver)
        {
        }

        public RoutingHost(IDependencyResolver resolver)
            : base(resolver)
        {
            _routeManager = new RouteManager(resolver);
        }

        /// <summary>
        /// Map the <see cref="T:Microsoft.AspNet.SignalR.Hubs.HubDisptcher"/> to the default hub url (~/signalr)
        /// </summary>
        public RoutingHost MapHubs()
        {
            return MapHubs("/signalr");
        }

        /// <summary>
        /// Maps the <see cref="T:Microsoft.AspNet.SignalR.Hubs.HubDisptcher"/> to the specified path.
        /// </summary>
        /// <param name="path">The path of the <see cref="T:Microsoft.AspNet.SignalR.Hubs.HubDisptcher"/></param>
        public RoutingHost MapHubs(string path)
        {
            _routeManager.MapHubs(path);

            return this;
        }

        /// <summary>
        /// Maps the url to the specified <see cref="PersistentConnection"/>.
        /// </summary>
        /// <typeparam name="TConnection">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="path">The path of <see cref="PersistentConnection"/></param>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public RoutingHost MapConnection<TConnection>(string path) where TConnection : PersistentConnection
        {
            _routeManager.MapConnection<TConnection>(path);
            return this;
        }

        public bool TryGetConnection(string path, out PersistentConnection connection)
        {
            return _routeManager.TryGetConnection(path, out connection);
        }
    }
}
