using System;
using System.Web.Routing;
using SignalR.Infrastructure;

namespace SignalR.Hosting.AspNet.Routing
{
    public static class RouteExtensions
    {
        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url) where T : PersistentConnection
        {
            return MapConnection(routes, name, url, typeof(T), Global.DependencyResolver);
        }

        public static RouteBase MapConnection<T>(this RouteCollection routes, string name, string url, IDependencyResolver resolver) where T : PersistentConnection
        {
            return MapConnection(routes, name, url, typeof(T));
        }

        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type)
        {
            return MapConnection(routes, name, url, type, Global.DependencyResolver);
        }

        public static RouteBase MapHubs(this RouteCollection routes, string name, string url)
        {
            return MapHubs(routes, name, url, Global.DependencyResolver);
        }

        public static RouteBase MapHubs(this RouteCollection routes, string name, string url, IDependencyResolver resolver)
        {
            string routeUrl = url;
            if (!routeUrl.EndsWith("/"))
            {
                routeUrl += "/{*operation}";
            }

            routeUrl = routeUrl.TrimStart('~').TrimStart('/');

            var route = new Route(routeUrl, new HubDispatcherRouteHandler(url, resolver));
            route.Constraints = new RouteValueDictionary();
            route.Constraints.Add("Incoming", new IncomingOnlyRouteConstraint());
            route.Constraints.Add("IgnoreJs", new IgnoreJsRouteConstraint());
            routes.Add(name, route);
            return route;
        }

        public static RouteBase MapConnection(this RouteCollection routes, string name, string url, Type type, IDependencyResolver resolver)
        {
            var route = new Route(url, new PersistentRouteHandler(type, resolver));
            route.Constraints = new RouteValueDictionary();
            route.Constraints.Add("Incoming", new IncomingOnlyRouteConstraint());
            routes.Add(name, route);
            return route;
        }
    }
}
