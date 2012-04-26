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

        public IPersistentConnectionContext GetConnectionContext<T>() where T : PersistentConnection
        {
            return GetConnection(typeof(T));
        }

        public IPersistentConnectionContext GetConnection(Type type)
        {
            string connectionName = type.FullName;
            IConnection connection = GetConnection(connectionName);

            return new PersistentConnectionContext(connection, new GroupManager(connection, connectionName));
        }

        public IHubContext GetHubContext<T>() where T : IHub
        {
            return GetHubContext(typeof(T).GetHubName());
        }

        public IHubContext GetHubContext(string hubName)
        {
            var connection = GetConnection(connectionName: null);
            var hubManager = _resolver.Resolve<IHubManager>();
            HubDescriptor hubDescriptor = hubManager.EnsureHub(hubName);

            dynamic clients = new ClientAgent(connection, hubDescriptor.Name);

            return new HubContext(clients, new GroupManager(connection, hubName));
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
