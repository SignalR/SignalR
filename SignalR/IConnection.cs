using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// A communication channel for a <see cref="PersistentConnection"/> and its connections.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// The main signal for this connection. This is the main signalr for a <see cref="PersistentConnection"/>.
        /// </summary>
        string DefaultSignal { get; }

        /// <summary>
        /// Publishes a message to connections subscribed to the signal.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <returns>A task that returns when the message has be published</returns>
        Task Publish(ConnectionMessage message);
    }
}