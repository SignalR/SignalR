using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web;

namespace SignalR.Routing {
    public class IncomingOnlyRouteConstraint : IRouteConstraint {
        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection) {
            if (routeDirection == RouteDirection.IncomingRequest) {
                return true;
            }
            return false;
        }
    }
}
