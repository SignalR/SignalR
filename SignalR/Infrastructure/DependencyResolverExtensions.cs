using System;
using System.Linq;
using System.Threading.Tasks;
using SignalR.Hubs;

namespace SignalR.Infrastructure
{
    public static class DependencyResolverExtensions
    {
        public static T Resolve<T>(this IDependencyResolver resolver)
        {
            return (T)resolver.GetService(typeof(T));
        }

        public static object Resolve(this IDependencyResolver resolver, Type type)
        {
            return resolver.GetService(type);
        }

        public static IConnection GetConnection<T>(this IDependencyResolver resolver) where T : PersistentConnection
        {
            return GetConnection(resolver, typeof(T));
        }

        public static IConnection GetConnection(this IDependencyResolver resolver, Type type)
        {
            return GetConnection(resolver, type.FullName);
        }

        public static dynamic GetClients<T>(this IDependencyResolver resolver) where T : IHub
        {
            return GetClients(resolver, typeof(T).FullName);
        }

        public static dynamic GetClients(this IDependencyResolver resolver, string hubName)
        {
            var connection = GetConnection<HubDispatcher>(resolver);
            return new ClientAgent(connection, hubName);
        }

        public static Task CloseConnections(this IDependencyResolver resolver)
        {
            // Get the connection that represents all clients (even if the type really means nothing
            // since we're just broadcasting to all connected clients
            var connection = resolver.GetConnection<PersistentConnection>();

            // We're targeting all clients
            string key = SignalCommand.AddCommandSuffix(typeof(PersistentConnection).FullName);

            // Tell them all to go away
            var command = new SignalCommand
            {
                Type = CommandType.Timeout
            };

            return connection.Broadcast(key, command);
        }

        private static IConnection GetConnection(IDependencyResolver resolver, string connectionType)
        {
            return new Connection(resolver.Resolve<IMessageBus>(),
                                  resolver.Resolve<IJsonSerializer>(),
                                  connectionType,
                                  null,
                                  new[] { connectionType },
                                  Enumerable.Empty<string>(),
                                  resolver.Resolve<ITraceManager>());
        }
    }
}
