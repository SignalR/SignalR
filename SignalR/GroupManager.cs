using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class GroupManager : IGroupManager
    {
        private readonly IConnection _connection;
        private readonly string _groupPrefix;
        
        public GroupManager(IConnection connection, string groupPrefix)
        {
            _connection = connection;
            _groupPrefix = groupPrefix;
        }

        public Task Send(string groupName, object value)
        {
            return _connection.Send(CreateQualifiedName(groupName), value);
        }

        public Task Add(string connectionId, string groupName)
        {
            return _connection.SendCommand(CreateQualifiedName(connectionId), new SignalCommand
            {
                Type = CommandType.AddToGroup,
                Value = CreateQualifiedName(groupName)
            });
        }

        public Task Remove(string connectionId, string groupName)
        {
            return _connection.SendCommand(CreateQualifiedName(connectionId), new SignalCommand
            {
                Type = CommandType.RemoveFromGroup,
                Value = CreateQualifiedName(groupName)
            });
        }

        private string CreateQualifiedName(string groupName)
        {
            return _groupPrefix + "." + groupName;
        }
    }
}
