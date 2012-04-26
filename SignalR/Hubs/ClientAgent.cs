using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class ClientAgent : DynamicObject, IGroupManager
    {
        private readonly IConnection _connection;
        private readonly string _hubName;

        public ClientAgent(IConnection connection, string hubName)
        {
            _connection = connection;
            _hubName = hubName;
        }

        public dynamic this[string key]
        {
            get
            {
                return new SignalAgent(_connection, key, _hubName);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = Invoke(binder.Name, args);
            return true;
        }

        public Task Add(string connectionId, string groupName)
        {
            return SendCommand(connectionId, 
                               CommandType.AddToGroup, 
                               CreateQualifiedName(groupName));
        }

        public Task Remove(string connectionId, string groupName)
        {
            return SendCommand(connectionId, 
                               CommandType.RemoveFromGroup, 
                               CreateQualifiedName(groupName));
        }
        
        Task IGroupManager.Send(string groupName, object value)
        {
            throw new NotSupportedException("Use the dynamic object to send messages to a specific group.");
        }
        
        private Task Invoke(string method, params object[] args)
        {
            var invocation = new
            {
                Hub = _hubName,
                Method = method,
                Args = args
            };

            return _connection.Send(_hubName, invocation);
        }

        private Task SendCommand(string connectionId, CommandType commandType, object commandValue)
        {
            string signal = SignalCommand.AddCommandSuffix(CreateQualifiedName(connectionId));

            var command = new SignalCommand
            {
                Type = commandType,
                Value = commandValue
            };

            return _connection.Send(signal, command);
        }

        private string CreateQualifiedName(string unqualifiedName)
        {
            return _hubName + "." + unqualifiedName;
        }
    }
}
