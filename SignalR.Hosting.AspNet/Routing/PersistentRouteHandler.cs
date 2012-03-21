using System;
using System.Web;
using System.Web.Routing;

namespace SignalR.Hosting.AspNet.Routing
{
    public class PersistentRouteHandler : IRouteHandler
    {
        private readonly Type _handlerType;
        private readonly IDependencyResolver _resolver;
        
        public PersistentRouteHandler(Type handlerType, IDependencyResolver resolver)
        {
            _handlerType = handlerType;
            _resolver = resolver;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            var factory = new PersistentConnectionFactory(_resolver);
            PersistentConnection connection = factory.CreateInstance(_handlerType);

            return new AspNetHandler(_resolver, connection);
        }
    }
}
