using System;
using System.Web;
using System.Web.Routing;
using SignalR.Infrastructure;

namespace SignalR.Routing
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
            var factory = (IPersistentConnectionFactory)_resolver.GetService(typeof(IPersistentConnectionFactory));
            return factory.CreateInstance(requestContext, _handlerType);
        }
    }
}
