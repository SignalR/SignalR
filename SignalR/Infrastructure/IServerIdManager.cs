namespace SignalR.Infrastructure
{
    /// <summary>
    /// Generates a server id
    /// </summary>
    public interface IServerIdManager
    {
        /// <summary>
        /// The id of the server.
        /// </summary>
        string ServerId { get; }
    }
}
