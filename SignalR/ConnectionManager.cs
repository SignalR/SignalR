using System;
using System.Linq;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// Default <see cref="IConnectionManager"/> implementation.
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private readonly IDependencyResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="resolver">The <see cref="IDependencyResolver"/>.</param>
        public ConnectionManager(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Returns a <see cref="IPersistentConnectionContext"/> for the <see cref="PersistentConnection"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="PersistentConnection"/></typeparam>
        /// <returns>A <see cref="IPersistentConnectionContext"/> for the <see cref="PersistentConnection"/>.</returns>
        public IPersistentConnectionContext GetConnectionContext<T>() where T : PersistentConnection
        {
            return GetConnection(typeof(T));
        }

        /// <summary>
        /// Returns a <see cref="IPersistentConnectionContext"/> for the <see cref="PersistentConnection"/>.
        /// </summary>
        /// <param name="type">Type of the <see cref="PersistentConnection"/></param>
        /// <returns>A <see cref="IPersistentConnectionContext"/> for the <see cref="PersistentConnection"/>.</returns>
        public IPersistentConnectionContext GetConnection(Type type)
        {
            string connectionName = type.FullName;
            IConnection connection = GetConnection(connectionName);

            return new PersistentConnectionContext(connection, new GroupManager(connection, connectionName));
        }

        /// <summary>
        /// Returns a <see cref="IHubContext"/> for the specified <see cref="IHub"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IHub"/></typeparam>
        /// <returns>a <see cref="IHubContext"/> for the specified <see cref="IHub"/></returns>
        public IHubContext GetHubContext<T>() where T : IHub
        {
            return GetHubContext(typeof(T).GetHubName());
        }

        /// <summary>
        /// Returns a <see cref="IHubContext"/>for the specified hub.
        /// </summary>
        /// <param name="hubName">Name of the hub</param>
        /// <returns>a <see cref="IHubContext"/> for the specified hub</returns>
        public IHubContext GetHubContext(string hubName)
        {
            var connection = GetConnection(connectionName: null);
            var hubManager = _resolver.Resolve<IHubManager>();
            HubDescriptor hubDescriptor = hubManager.EnsureHub(hubName);

            return new HubContext(new ClientProxy(connection, hubDescriptor.Name), 
                                  new GroupManager(connection, hubName));
        }

        private IConnection GetConnection(string connectionName)
        {
            var signals = connectionName == null ? Enumerable.Empty<string>() : new[] { connectionName };

            // Give this a unique id
            var connectionId = Guid.NewGuid().ToString();
            return new Connection(_resolver.Resolve<IMessageBus>(),
                                  _resolver.Resolve<INewMessageBus>(),
                                  _resolver.Resolve<IJsonSerializer>(),
                                  connectionName,
                                  connectionId,
                                  signals,
                                  Enumerable.Empty<string>(),
                                  _resolver.Resolve<ITraceManager>());
        }
    }
}
