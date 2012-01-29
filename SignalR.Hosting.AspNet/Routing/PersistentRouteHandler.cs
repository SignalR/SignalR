using System;
using System.Web;
using System.Web.Routing;
using SignalR.Infrastructure;

namespace SignalR.Hosting.AspNet.Routing
{
    public class PersistentRouteHandler : IRouteHandler
    {
        private readonly Type _handlerType;
        private readonly IDependencyResolver _resolver;

        public PersistentRouteHandler(IDependencyResolver resolver, Type handlerType)
        {
            _resolver = resolver;
            _handlerType = handlerType;
        }

        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            var factory = new PersistentConnectionFactory(_resolver);
            PersistentConnection connection = factory.CreateInstance(_handlerType);

            // Initialize the connection
            connection.Initialize(_resolver);

            return new AspNetHost(connection);
        }
    }
}
