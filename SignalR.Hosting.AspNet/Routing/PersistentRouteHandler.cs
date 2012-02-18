using System;
using System.Web;
using System.Web.Routing;

namespace SignalR.Hosting.AspNet.Routing
{
    public class PersistentRouteHandler : IRouteHandler
    {
        private readonly Type _handlerType;
        
        public PersistentRouteHandler(Type handlerType)
        {
            _handlerType = handlerType;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            var factory = new PersistentConnectionFactory(AspNetHost.DependencyResolver);
            PersistentConnection connection = factory.CreateInstance(_handlerType);

            return new AspNetHost(connection);
        }
    }
}
