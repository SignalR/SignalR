using SignalR.Hubs;

namespace SignalR
{
    /// <summary>
    /// Provides access to hubs and persistent connections.
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Returns a dynamic object representing all clients connected to the specified <see cref="IHub"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="IHub"/></typeparam>
        /// <returns>a dynamic object representing all clients connected to the specified <see cref="IHub"/></returns>
        dynamic GetClients<T>() where T : IHub;

        /// <summary>
        /// Returns a dynamic object representing all clients connected to the specified hub.
        /// </summary>
        /// <param name="hubName">Name of the hub</param>
        /// <returns>a dynamic object representing all clients connected to the specified hub</returns>
        dynamic GetClients(string hubName);

        /// <summary>
        /// Returns a connection representing all clients connected to the specified <see cref="PersistentConnection"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="PersistentConnection"/></typeparam>
        /// <returns>An IConnection representing all clients connected to the specified <see cref="PersistentConnection"/></returns>
        IConnection GetConnection<T>() where T : PersistentConnection;
    }
}
