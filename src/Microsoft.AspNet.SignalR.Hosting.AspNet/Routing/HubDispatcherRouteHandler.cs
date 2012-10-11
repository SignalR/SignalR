using System;
using System.Web;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.Hosting.AspNet.Infrastructure;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Routing
{
    public class HubDispatcherRouteHandler : IRouteHandler
    {
        private readonly string _url;
        private readonly IDependencyResolver _resolver;

        public HubDispatcherRouteHandler(string url, IDependencyResolver resolver)
        {
            _url = VirtualPathUtility.ToAbsolute(url);
            _resolver = resolver;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {            
            var dispatcher = new HubDispatcher(_url);
            return new AspNetHandler(_resolver, dispatcher);
        }
    }
}
