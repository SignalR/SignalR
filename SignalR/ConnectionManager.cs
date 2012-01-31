using System;
using System.Linq;
using System.Threading.Tasks;
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
            return GetClients(typeof(T).FullName);
        }

        public dynamic GetClients(string hubName)
        {
            var connection = GetConnection<HubDispatcher>();
            return new ClientAgent(connection, hubName);
        }

        public Task CloseConnections(string scope)
        {
            // Get the connection that represents all clients (even if the type really means nothing
            // since we're just broadcasting to all connected clients
            var connection = GetConnection<PersistentConnection>();

            // We're targeting all clients
            string key = SignalCommand.AddCommandSuffix(scope);

            // Tell them all to go away
            var command = new SignalCommand
            {
                Type = CommandType.Timeout
            };

            return connection.Broadcast(key, command);
        }

        private IConnection GetConnection(string connectionType)
        {
            return new Connection(_resolver.Resolve<IMessageBus>(),
                                  _resolver.Resolve<IJsonSerializer>(),
                                  connectionType,
                                  null,
                                  new[] { connectionType },
                                  Enumerable.Empty<string>(),
                                  _resolver.Resolve<ITraceManager>());
        }
    }
}
