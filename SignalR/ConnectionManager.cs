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

        public IConnection GetConnection<T>() where T : PersistentConnection
        {
            return GetConnection(typeof(T));
        }

        public IConnection GetConnection(Type type)
        {
            return GetConnection(type.FullName);
        }

        public dynamic GetClients<T>() where T : IHub
        {
            return GetClients(typeof(T).GetHubName());
        }

        public dynamic GetClients(string hubName)
        {
            var connection = GetConnection<HubDispatcher>();
            var hubManager = _resolver.Resolve<IHubManager>();
            HubDescriptor hubDescriptor = hubManager.EnsureHub(hubName);

            return new ClientAgent(connection, hubDescriptor.Name);
        }

        private IConnection GetConnection(string connectionType)
        {
            // Give this a unique id
            var connectionId = Guid.NewGuid().ToString();
            return new Connection(_resolver.Resolve<IMessageBus>(),
                                  _resolver.Resolve<IJsonSerializer>(),
                                  connectionType,
                                  connectionId,
                                  new[] { connectionType },
                                  Enumerable.Empty<string>(),
                                  _resolver.Resolve<ITraceManager>());
        }
    }
}
