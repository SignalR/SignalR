using System;
using System.Web;
using System.Web.Routing;
using SignalR.Hubs;

namespace SignalR.Hosting.AspNet.Routing
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
            // REVIEW: This only needs to be done once per resolver.
            var hubLocator = new Lazy<BuildManagerTypeLocator>(() => new BuildManagerTypeLocator());
            var typeResolver = new Lazy<BuildManagerTypeResolver>(() => new BuildManagerTypeResolver(hubLocator.Value));

            _resolver.Register(typeof(IHubLocator), () => hubLocator.Value);
            _resolver.Register(typeof(IHubTypeResolver), () => typeResolver.Value);

            var dispatcher = new HubDispatcher(_url);

            return new AspNetHandler(_resolver, dispatcher);
        }
    }
}
