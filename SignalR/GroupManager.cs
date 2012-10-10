using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    /// <summary>
    /// The default <see cref="IGroupManager"/> implementation.
    /// </summary>
    public class GroupManager : IConnectionGroupManager
    {
        private readonly IConnection _connection;
        private readonly string _groupPrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupManager"/> class.
        /// </summary>
        /// <param name="connection">The <see cref="IConnection"/> this group resides on.</param>
        /// <param name="groupPrefix">The prefix for this group. Either a <see cref="IHub"/> name or <see cref="PersistentConnection"/> type name.</param>
        public GroupManager(IConnection connection, string groupPrefix)
        {
            _connection = connection;
            _groupPrefix = groupPrefix;
        }

        /// <summary>
        /// Sends a value to the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <param name="value">The value to send.</param>
        /// <returns>A task that represents when send is complete.</returns>
        public Task Send(string groupName, object value, params string[] exclude)
        {
            var qualifiedName = CreateQualifiedName(groupName);
            var message = new ConnectionMessage(qualifiedName, value)
            {
                ExcludedSignals = exclude
            };

            return _connection.Send(message);
        }

        /// <summary>
        /// Adds a connection to the specified group. 
        /// </summary>
        /// <param name="connectionId">The connection id to add to the group.</param>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A task that represents the connection id being added to the group.</returns>
        public Task Add(string connectionId, string groupName)
        {
            var command = new Command
            {
                Type = CommandType.AddToGroup,
                Value = CreateQualifiedName(groupName),
                WaitForAck = true
            };

            return _connection.Send(connectionId, command);
        }

        /// <summary>
        /// Removes a connection from the specified group.
        /// </summary>
        /// <param name="connectionId">The connection id to remove from the group.</param>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A task that represents the connection id being removed from the group.</returns>
        public Task Remove(string connectionId, string groupName)
        {
            var command = new Command
            {
                Type = CommandType.RemoveFromGroup,
                Value = CreateQualifiedName(groupName),
                WaitForAck = true
            };

            return _connection.Send(connectionId, command);
        }

        private string CreateQualifiedName(string groupName)
        {
            return _groupPrefix + "." + groupName;
        }
    }
}
