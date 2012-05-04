using System.Threading.Tasks;

namespace SignalR.Hubs
{
    /// <summary>
    /// Enables disconnect notificatins for a <see cref="IHub"/>
    /// </summary>
    /// <example>
    /// public class MyHub : Hub, IDisconnect
    /// {
    ///     public Task Disconnect()
    ///     {
    ///         // Tell everyone this connection is gone
    ///         return Clients.notifyLeave(Context.ConnectionId);
    ///     }
    /// }
    /// </example>
    public interface IDisconnect
    {
        /// <summary>
        /// Called when a connection is disconnected from the <see cref="IHub"/>.
        /// </summary>
        /// <remarks>
        /// This method is invoked from the server side which means the only valid property on the <see cref="HubCallerContext"/>
        /// is the connection id.
        /// </remarks>
        Task Disconnect();
    }
}
