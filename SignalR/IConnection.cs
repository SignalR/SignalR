using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// A communication channel for a <see cref="PersistentConnection"/> and its connections.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// The Broadcast signal
        /// </summary>
        string DefaultSignal { get; }

        /// <summary>
        /// Publishes a message 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Publish(ConnectionMessage message);
    }
}