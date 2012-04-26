using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// Manages groups for a connection.
    /// </summary>
    public interface IGroupManager
    {
        /// <summary>
        /// Sends a value to the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <param name="value">The value to send.</param>
        /// <returns>A task that represents when send is complete.</returns>
        Task Send(string groupName, object value);

        /// <summary>
        /// Adds a connection to the specified group. 
        /// </summary>
        /// <param name="connectionId">The connection id to add to the group.</param>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A task that represents the connection id being added to the group.</returns>
        Task Add(string connectionId, string groupName);

        /// <summary>
        /// Removes a connection from the specified group.
        /// </summary>
        /// <param name="connectionId">The connection id to remove from the group.</param>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A task that represents the connection id being removed from the group.</returns>
        Task Remove(string connectionId, string groupName);
    }
}
