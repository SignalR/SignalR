using System;
using System.IO;
using System.Web;
using System.Web.Routing;

namespace SignalR.Hosting.AspNet.Routing
{
    public class IgnoreJsRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (Path.GetExtension(httpContext.Request.AppRelativeCurrentExecutionFilePath).Equals(".js", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
