using System;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class PersistentConnectionGroupManager : IGroupManager
    {
        private readonly IConnection _connection;
        private readonly string _defaultSignal;

        public PersistentConnectionGroupManager(IConnection connection, Type connectionType)
        {
            _connection = connection;
            _defaultSignal = connectionType.FullName;
        }

        /// <summary>
        /// Sends a value to all connections in the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group.</param>
        /// <param name="value">The value to send.</param>
        /// <returns>A task that represents when the send is complete.</returns>
        public Task SendToGroup(string groupName, object value)
        {
            return _connection.Send(CreateQualifiedName(groupName), value);
        }

        /// <summary>
        /// Adds a connection to the specified group. 
        /// </summary>
        /// <param name="connectionId">The connection id to add to the group.</param>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>A task that represents the connection id being added to the group.</returns>
        public Task AddToGroup(string connectionId, string groupName)
        {
            groupName = CreateQualifiedName(groupName);
            return _connection.SendCommand(connectionId, new SignalCommand
            {
                Type = CommandType.AddToGroup,
                Value = groupName
            });
        }

        /// <summary>
        /// Removes a connection from the specified group.
        /// </summary>
        /// <param name="connectionId">The connection id to remove from the group.</param>
        /// <param name="groupName">The name of the group.</param>
        /// <returns>A task that represents the connection id being removed from the group.</returns>
        public Task RemoveFromGroup(string connectionId, string groupName)
        {
            groupName = CreateQualifiedName(groupName);
            return _connection.SendCommand(connectionId, new SignalCommand
            {
                Type = CommandType.RemoveFromGroup,
                Value = groupName
            });
        }

        private string CreateQualifiedName(string groupName)
        {
            return _defaultSignal + "." + groupName;
        }
    }
}
