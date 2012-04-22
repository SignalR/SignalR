using SignalR.Hosting.Common.Routing;

namespace SignalR.Hosting.Common
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
        /// Map the <see cref="HubDisptcher"/> to the default hub url (~/signalr)
        /// </summary>
        public RoutingHost MapHubs()
        {
            return MapHubs("/signalr");
        }

        /// <summary>
        /// Maps the <see cref="HubDisptcher"/> to the specified path.
        /// </summary>
        /// <param name="path">The path of the <see cref="HubDisptcher"/></param>
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
