using System.Threading.Tasks;

namespace SignalR
{
    public interface IConnection : IReceivingConnection
    {
        /// <summary>
        /// Sends a message to all connections waiting for the specified signal.
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