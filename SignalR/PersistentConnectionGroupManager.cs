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

        public Task SendToGroup(string groupName, object value)
        {
            return _connection.Send(CreateQualifiedName(groupName), value);
        }

        public Task AddToGroup(string connectionId, string groupName)
        {
            return _connection.SendCommand(connectionId, new SignalCommand
            {
                Type = CommandType.AddToGroup,
                Value = CreateQualifiedName(groupName)
            });
        }

        public Task RemoveFromGroup(string connectionId, string groupName)
        {
            return _connection.SendCommand(connectionId, new SignalCommand
            {
                Type = CommandType.RemoveFromGroup,
                Value = CreateQualifiedName(groupName)
            });
        }

        private string CreateQualifiedName(string groupName)
        {
            return _defaultSignal + "." + groupName;
        }
    }
}
