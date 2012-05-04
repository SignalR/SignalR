using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// Handles communication across SignalR connections.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// Returns a list of messages after the specified cursor or waits for new messages until the timeout is fired if there's no messages.
        /// </summary>
        /// <param name="eventKeys">A list of events to wait for messages.</param>
        /// <param name="cursor">A cursor representing how much data the caller has already seen.</param>
        /// <param name="timeoutToken">A <see cref="CancellationToken"/> that represents the timeout of the wait operation when there's no messages.</param>
        /// <returns>A <see cref="Task{MessageResult}"/> that completes when data is received on the <see cref="IMessageBus"/>.</returns>
        Task<MessageResult> GetMessages(IEnumerable<string> eventKeys, string cursor, CancellationToken timeoutToken);

        /// <summary>
        /// Sends a new message to the specified event on the bus.
        /// </summary>
        /// <param name="source">A value representing the source of the data sent.</param>
        /// <param name="eventKey">The specific event key to send data to.</param>
        /// <param name="value">The value to send.</param>
        /// <returns>A <see cref="Task"/> that completes when the send is complete.</returns>
        Task Send(string source, string eventKey, object value);
    }
}
