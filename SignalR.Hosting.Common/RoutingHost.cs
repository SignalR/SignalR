using SignalR.Hosting.Common.Routing;

namespace SignalR.Hosting.Common
{
    public class RoutingHost : Host
    {
        private readonly RouteManager _routeManager;

        public RoutingHost()
            : this(Global.DependencyResolver)
        {
        }

        public RoutingHost(IDependencyResolver resolver)
            : base(resolver)
        {
            _routeManager = new RouteManager(resolver);
        }

        public RoutingHost MapHubs()
        {
            return MapHubs("/signalr");
        }

        public RoutingHost MapHubs(string path)
        {
            _routeManager.MapHubs(path);

            return this;
        }

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
