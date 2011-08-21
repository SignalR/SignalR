using System;
using System.Web.Routing;
using SignalR.Infrastructure;

namespace SignalR.Routing {
    public static class RouteExtensions {
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url) where T : PersistentConnection {
            return MapConnection(routes, name, url, typeof(T));
        }

        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type) {
            var route = new Route(url, new PersistentRouteHandler(DependencyResolver.Current, type));
            routes.Add(name, route);
            return route;
        }
    }
}
