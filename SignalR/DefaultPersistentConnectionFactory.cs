using System;
using System.Web.Routing;
using SignalR.Infrastructure;

namespace SignalR
{
    public class DefaultPersistentConnectionFactory : IPersistentConnectionFactory
    {
        public PersistentConnection CreateInstance(RequestContext requestContext, Type connectionType)
        {
            if (connectionType == null)
            {
                throw new ArgumentNullException("connectionType");
            }

            var connection = (DependencyResolver.Resolve(connectionType) ??
                              Activator.CreateInstance(connectionType)) as PersistentConnection;

            if (connection == null)
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a {1}.", connectionType.FullName, typeof(PersistentConnection).FullName));
            }

            return connection;
        }
    }
}
