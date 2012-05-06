using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// A communication channel for a <see cref="PersistentConnection"/> and its connections.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Sends a message to all connections waiting for the specified signal. An example of signal may be a
        /// specific connection id, or fully qualified group name (Use <see cref="IGroupManager"/> to manipulate groups).
        /// </summary>
        /// <param name="signal">The signal to broacast to</param>
        /// <param name="value">The value to broadcast.</param>
        /// <returns>A task that represents when the broadcast is complete.</returns>
        Task Send(string signal, object value);

        /// <summary>
        /// Broadcasts a value to all connections.
        /// </summary>
        /// <param name="value">The value to broadcast.</param>
        /// <returns>A task that represents when the broadcast is complete.</returns>
        Task Broadcast(object value);
    }
}