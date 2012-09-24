﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Samples.Raw
{
    public class Raw : PersistentConnection
    {
        private static readonly ConcurrentDictionary<string, string> _users = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, string> _clients = new ConcurrentDictionary<string, string>();

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            var userNameCookie = request.Cookies["user"];
            if (userNameCookie != null)
            {
                _clients[connectionId] = userNameCookie.Value;
                _users[userNameCookie.Value] = connectionId;
            }

            string clientIp = request.ServerVariables["REMOTE_ADDR"];
            string user = GetUser(connectionId);

            return Groups.Add(connectionId, "foo").ContinueWith(_ =>
                   Connection.Broadcast(DateTime.Now + ": " + user + " joined from " + clientIp)).Unwrap();
        }

        protected override Task OnReconnectedAsync(IRequest request, string connectionId)
        {
            string user = GetUser(connectionId);

            return Connection.Broadcast(DateTime.Now + ": " + user + " reconnected");
        }

        protected override Task OnDisconnectAsync(string connectionId)
        {
            string ignored;
            _users.TryRemove(connectionId, out ignored);
            return Connection.Broadcast(DateTime.Now + ": " + GetUser(connectionId) + " disconnected");
        }

        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            var message = JsonConvert.DeserializeObject<Message>(data);

            switch (message.Type)
            {
                case MessageType.Broadcast:
                    Connection.Broadcast(new
                    {
                        type = MessageType.Broadcast,
                        from = GetUser(connectionId),
                        data = message.Value
                    });
                    break;
                case MessageType.Send:
                    Connection.Send(connectionId, new
                    {
                        type = MessageType.Send,
                        from = GetUser(connectionId),
                        data = message.Value
                    });
                    break;
                case MessageType.Join:
                    string name = message.Value;
                    _clients[connectionId] = name;
                    _users[name] = connectionId;
                    Connection.Send(connectionId, new
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
                    Connection.Send(id, new
                    {
                        from = GetUser(connectionId),
                        data = msg
                    });
                    break;
                case MessageType.AddToGroup:
                    Groups.Add(connectionId, message.Value);
                    break;
                case MessageType.RemoveFromGroup:
                    Groups.Remove(connectionId, message.Value);
                    break;
                case MessageType.SendToGroup:
                    var parts2 = message.Value.Split('|');
                    string groupName = parts2[0];
                    string val = parts2[1];
                    Groups.Send(groupName, val);
                    break;
                default:
                    break;
            }

            return base.OnReceivedAsync(request, connectionId, data);
        }

        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return groups;
        }

        private string GetUser(string connectionId)
        {
            string user;
            if (!_clients.TryGetValue(connectionId, out user))
            {
                return connectionId;
            }
            return user;
        }

        private string GetClient(string user)
        {
            string connectionId;
            if (_users.TryGetValue(user, out connectionId))
            {
                return connectionId;
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