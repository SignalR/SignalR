using System;
using SignalR.Hosting.Common.Routing;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR.Hosting.Common
{
    public class RoutingHost : Host
    {
        private readonly RouteManager _routeManager;

        public RoutingHost()
            : base()
        {
        }

        public RoutingHost(IDependencyResolver resolver)
            : base(resolver)
        {
            _routeManager = new RouteManager(resolver);
        }

        public void MapConnection<TConnection>(string path) where TConnection : PersistentConnection
        {
            _routeManager.MapConnection<TConnection>(path);
        }

        public virtual bool TryGetConnection(string path, out PersistentConnection connection)
        {
            if (path.StartsWith("/signalr", StringComparison.OrdinalIgnoreCase))
            {
                connection = new HubDispatcher("/signalr");
                return true;
            }

            return _routeManager.TryGetConnection(path, out connection);
        }
    }
}
