using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    /// <summary>
    /// Enables connect and reconnect notifications for a <see cref="IHub"/>
    /// </summary>
    /// <example>
    /// public class MyHub : Hub, IConnected
    /// {
    ///     public Task Connect()
    ///     {
    ///         return Clients.notifyClient("new connection established for "+ Context.ConnectionId);
    ///     }
    ///     
    ///     public Task Reconnect(IEnumerable{string} groups)
    ///     {
    ///         return Clients.notifyClient("connection re-established for "+ Context.ConnectionId);
    ///     }
    /// }
    /// </example>
    public interface IConnected
    {
        /// <summary>
        /// Called when a new connection is made to the <see cref="IHub"/>.
        /// </summary>
        Task Connect();

        /// <summary>
        /// Called when a connection reconnects to the <see cref="IHub"/> after a timeout.
        /// </summary>
        /// <param name="groups">The groups the connection is a member of.</param>
        Task Reconnect();

        /// <summary>
        /// Called before a connection completes reconnecting to the <see cref="IHub"/> after a timeout.
        /// </summary>
        /// <param name="groups">The groups the reconnecting client claims to be a member of.</param>
        /// <returns>The groups the client will actually join.</returns>
        IEnumerable<string> RejoiningGroups(IEnumerable<string> groups);
    }
}
