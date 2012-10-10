using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// Manages groups for a connection and allows sending messages to the group.
    /// </summary>
    public interface IConnectionGroupManager : IGroupManager
    {
        /// <summary>
        /// Sends a value to the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <param name="value">The value to send.</param>
        /// <param name="exclude">List of connection ids to exclude</param>
        /// <returns>A task that represents when send is complete.</returns>
        Task Send(string groupName, object value, params string[] exclude);
    }
}
