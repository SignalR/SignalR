using System;
using SignalR.Infrastructure;

namespace SignalR
{
    public class PersistentConnectionFactory
    {
        private readonly IDependencyResolver _resolver;

        public PersistentConnectionFactory(IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            _resolver = resolver;
        }

        public PersistentConnection CreateInstance(Type connectionType)
        {
            if (connectionType == null)
            {
                throw new ArgumentNullException("connectionType");
            }

            var connection = (_resolver.Resolve(connectionType) ??
                              Activator.CreateInstance(connectionType)) as PersistentConnection;

            if (connection == null)
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a {1}.", connectionType.FullName, typeof(PersistentConnection).FullName));
            }

            return connection;
        }
    }
}
