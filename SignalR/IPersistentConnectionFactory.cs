using System;
using System.Web.Routing;

namespace SignalR
{
    public interface IPersistentConnectionFactory
    {
        PersistentConnection CreateInstance(RequestContext requestContext, Type handlerType);
    }
}
