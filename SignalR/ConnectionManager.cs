using System;
using System.Linq;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly IDependencyResolver _resolver;

        public ConnectionManager(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public PersistentConnectionContext GetConnectionContext<T>() where T : PersistentConnection
        {
            return GetConnection(typeof(T));
        }

        public PersistentConnectionContext GetConnection(Type type)
        {
            string connectionName = type.FullName;
            IConnection connection = GetConnection(connectionName);

            return new PersistentConnectionContext(connection, new GroupManager(connection, connectionName));
        }

        public dynamic GetClients<T>() where T : IHub
        {
            return GetClients(typeof(T).GetHubName());
        }

        public dynamic GetClients(string hubName)
        {
            var connection = GetConnection(connectionName: null);
            var hubManager = _resolver.Resolve<IHubManager>();
            HubDescriptor hubDescriptor = hubManager.EnsureHub(hubName);
            
            return new ClientAgent(connection, hubDescriptor.Name);
        }

        private IConnection GetConnection(string connectionName)
        {
            var signals = connectionName == null ? Enumerable.Empty<string>() : new[] { connectionName };

            // Give this a unique id
            var connectionId = Guid.NewGuid().ToString();
            return new Connection(_resolver.Resolve<IMessageBus>(),
                                  _resolver.Resolve<IJsonSerializer>(),
                                  connectionName,
                                  connectionId,
                                  signals,
                                  Enumerable.Empty<string>(),
                                  _resolver.Resolve<ITraceManager>());
        }
    }
}
