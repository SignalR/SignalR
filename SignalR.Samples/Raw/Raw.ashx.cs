using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace SignalR.Samples.Raw
{
    public class Raw : PersistentConnection
    {
        private static readonly Dictionary<string, string> _users = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _clients = new Dictionary<string, string>();

        protected override Task OnConnectedAsync(HttpContextBase context, string clientId)
        {
            var cookie = context.Request.Cookies["user"];
            if (cookie != null)
            {
                _clients[clientId] = cookie.Value;
                _users[cookie.Value] = clientId;
            }

            string user = GetUser(clientId);

            return Connection.Broadcast(user + " joined");
        }

        protected override Task OnDisconnectAsync(string clientId)
        {
            _users.Remove(clientId);
            return Connection.Broadcast(GetUser(clientId) + " disconnected");
        }

        protected override void OnReceived(string clientId, string data)
        {
            var serializer = new JavaScriptSerializer();
            var message = serializer.Deserialize<Message>(data);

            switch (message.Type)
            {
                case MessageType.Broadcast:
                    Connection.Broadcast(new
                    {
                        type = MessageType.Broadcast,
                        from = GetUser(clientId),
                        data = message.Value
                    });
                    break;
                case MessageType.Send:
                    Send(new
                    {
                        type = MessageType.Send,
                        from = GetUser(clientId),
                        data = message.Value
                    });
                    break;
                case MessageType.Join:
                    string name = message.Value;
                    _clients[clientId] = name;
                    _users[name] = clientId;
                    Send(new
                    {
                        type = MessageType.Join,
                        data = message.Value
                    });
                    break;
                case MessageType.PrivateMessage:
                    var parts = message.Value.Split('|');
                    string user = parts[0];
                    string msg = parts[1];
                    string id = GetClient(user);
                    Send(id, new
                    {
                        from = GetUser(clientId),
                        data = msg
                    });
                    break;
                case MessageType.AddToGroup:
                    AddToGroup(clientId, message.Value);
                    break;
                case MessageType.RemoveFromGroup:
                    RemoveFromGroup(clientId, message.Value);
                    break;
                case MessageType.SendToGroup:
                    var parts2 = message.Value.Split('|');
                    string groupName = parts2[0];
                    string val = parts2[1];
                    SendToGroup(groupName, val);
                    break;
                default:
                    break;
            }
        }

        private string GetUser(string clientId)
        {
            string user;
            if (!_clients.TryGetValue(clientId, out user))
            {
                return clientId;
            }
            return user;
        }

        private string GetClient(string user)
        {
            string clientId;
            if (_users.TryGetValue(user, out clientId))
            {
                return clientId;
            }
            return null;
        }

        enum MessageType
        {
            Send,
            Broadcast,
            Join,
            PrivateMessage,
            AddToGroup,
            RemoveFromGroup,
            SendToGroup
        }

        class Message
        {
            public MessageType Type { get; set; }
            public string Value { get; set; }
        }
    }
}