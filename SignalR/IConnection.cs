using System.Threading.Tasks;

namespace SignalR
{
    public interface IConnection : IReceivingConnection
    {
        /// <summary>
        /// Send a message to the incoming connection id associated with the <see cref="IConnection"/>.
        /// </summary>
        /// <param name="value">The value to send</param>
        /// <returns>A task that represents when the send is complete.</returns>
        Task Send(object value);

        /// <summary>
        /// Send a message to all connected clients waiting for the specified signal.
        /// </summary>
        /// <param name="signal">The signal to broacast to</param>
        /// <param name="value">The value to broadcast.</param>
        /// <returns>A task that represents when the broadcast is complete.</returns>        
        Task Send(string signal, object value);

        /// <summary>
        /// Broadcasts a value to all connected clients.
        /// </summary>
        /// <param name="value">The value to broadcast.</param>
        /// <returns>A task that represents when the broadcast is complete.</returns>
        Task Broadcast(object value);
    }
}