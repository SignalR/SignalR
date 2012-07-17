using System.Collections.Generic;

namespace SignalR.Transports
{
    /// <summary>
    /// Manages tracking the state of connections.
    /// </summary>
    public interface ITransportHeartBeat
    {
        /// <summary>
        /// Adds a new connection to the list of tracked connections.
        /// </summary>
        /// <param name="connection">The connection to be added.</param>
        bool AddConnection(ITrackingConnection connection);

        /// <summary>
        /// Marks an existing connection as active.
        /// </summary>
        /// <param name="connection">The connection to mark.</param>
        void MarkConnection(ITrackingConnection connection);

        /// <summary>
        /// Removes a connection from the list of tracked connections.
        /// </summary>
        /// <param name="connection">The connection to remove.</param>
        void RemoveConnection(ITrackingConnection connection);

        /// <summary>
        /// Gets a list of connections being tracked.
        /// </summary>
        /// <returns>A list of connections.</returns>
        IList<ITrackingConnection> GetConnections();
    }
}
