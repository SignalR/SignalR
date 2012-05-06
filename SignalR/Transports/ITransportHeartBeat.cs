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
        void AddConnection(ITrackingConnection connection);

        /// <summary>
        /// Updates an existing connection and it's metadata.
        /// </summary>
        /// <param name="connection">The connection to be updated.</param>
        void UpdateConnection(ITrackingConnection connection);

        /// <summary>
        /// Marks an existing connection as active.
        /// </summary>
        /// <param name="connection">The connection to mark.</param>
        void MarkConnection(ITrackingConnection connection);
    }
}
