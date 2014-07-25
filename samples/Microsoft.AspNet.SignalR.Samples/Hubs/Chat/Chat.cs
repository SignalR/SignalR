using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Samples.Hubs.Chat.ContentProviders;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Chat
{
    public class Chat : Hub
    {
        private static readonly ConcurrentDictionary<string, ChatUser> _users = new ConcurrentDictionary<string, ChatUser>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, HashSet<string>> _userRooms = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, ChatRoom> _rooms = new ConcurrentDictionary<string, ChatRoom>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<IContentProvider> _contentProviders = new List<IContentProvider>() {
            new ImageContentProvider(),
            new YouTubeContentProvider(),
            new CollegeHumorContentProvider()
        };

        public bool Join()
        {
            // Check the user id cookie
            Cookie userIdCookie;

            if (!Context.RequestCookies.TryGetValue("userid", out userIdCookie))
            {
                return false;
            }

            ChatUser user = _users.Values.FirstOrDefault(u => u.Id == userIdCookie.Value);

            if (user != null)
            {
                // Update the users's client id mapping
                user.ConnectionId = Context.ConnectionId;

                // Set some client state
                Clients.Caller.id = user.Id;
                Clients.Caller.name = user.Name;
                Clients.Caller.hash = user.Hash;

                // Leave all rooms
                HashSet<string> rooms;
                if (_userRooms.TryGetValue(user.Name, out rooms))
                {
                    foreach (var room in rooms)
                    {
                        Clients.Group(room).leave(user);
                        ChatRoom chatRoom = _rooms[room];
                        chatRoom.Users.Remove(user.Name);
                    }
                }

                _userRooms[user.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Add this user to the list of users
                Clients.Caller.addUser(user);
                return true;
            }

            return false;
        }

        public void Send(string content)
        {
            content = content.Replace("<", "&lt;").Replace(">", "&gt;");

            if (!TryHandleCommand(content))
            {
                string roomName = Clients.Caller.room;
                string name = Clients.Caller.name;

                EnsureUserAndRoom();

                HashSet<string> links;
                var messageText = Transform(content, out links);
                var chatMessage = new ChatMessage(name, messageText);

                _rooms[roomName].Messages.Add(chatMessage);

                Clients.Group(roomName).addMessage(chatMessage.Id, chatMessage.User, chatMessage.Text);

                if (links.Any())
                {
                    // REVIEW: is this safe to do? We're holding on to this instance 
                    // when this should really be a fire and forget.
                    var contentTasks = links.Select(ExtractContent).ToArray();
                    Task.Factory.ContinueWhenAll(contentTasks, tasks =>
                    {
                        foreach (var task in tasks)
                        {
                            if (task.IsFaulted)
                            {
                                Trace.TraceError(task.Exception.GetBaseException().Message);
                                continue;
                            }

                            if (String.IsNullOrEmpty(task.Result))
                            {
                                continue;
                            }

                            // Try to get content from each url we're resolved in the query
                            string extractedContent = "<p>" + task.Result + "</p>";

                            // If we did get something, update the message and notify all clients
                            chatMessage.Text += extractedContent;

                            Clients.Group(roomName).addMessageContent(chatMessage.Id, extractedContent);
                        }
                    });
                }
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            ChatUser user = _users.Values.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
            if (user != null)
            {
                ChatUser ignoredUser;
                _users.TryRemove(user.Name, out ignoredUser);

                // Leave all rooms
                HashSet<string> rooms;
                if (_userRooms.TryGetValue(user.Name, out rooms))
                {
                    foreach (var room in rooms)
                    {
                        Clients.Group(room).leave(user);
                        ChatRoom chatRoom = _rooms[room];
                        chatRoom.Users.Remove(user.Name);
                    }
                }

                HashSet<string> ignoredRoom;
                _userRooms.TryRemove(user.Name, out ignoredRoom);
            }

            return null;
        }

        public IEnumerable<ChatUser> GetUsers()
        {
            string room = Clients.Caller.room;

            if (String.IsNullOrEmpty(room))
            {
                return Enumerable.Empty<ChatUser>();
            }

            return from name in _rooms[room].Users
                   select _users[name];
        }

        private string GetMD5Hash(string name)
        {
            return String.Join("", MD5.Create()
                         .ComputeHash(Encoding.Default.GetBytes(name))
                         .Select(b => b.ToString("x2")));
        }

        private bool TryHandleCommand(string message)
        {
            string room = Clients.Caller.room;
            string name = Clients.Caller.name;

            message = message.Trim();
            if (message.StartsWith("/"))
            {
                string[] parts = message.Substring(1).Split(' ');
                string commandName = parts[0];

                if (commandName.Equals("nick", StringComparison.OrdinalIgnoreCase))
                {
                    string newUserName = String.Join(" ", parts.Skip(1));

                    if (String.IsNullOrEmpty(newUserName))
                    {
                        throw new InvalidOperationException("No username specified!");
                    }

                    if (newUserName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("That's already your username...");
                    }

                    if (!_users.ContainsKey(newUserName))
                    {
                        if (String.IsNullOrEmpty(name) || !_users.ContainsKey(name))
                        {
                            AddUser(newUserName);
                        }
                        else
                        {
                            var oldUser = _users[name];
                            var newUser = new ChatUser
                            {
                                Name = newUserName,
                                Hash = GetMD5Hash(newUserName),
                                Id = oldUser.Id,
                                ConnectionId = oldUser.ConnectionId
                            };

                            _users[newUserName] = newUser;
                            _userRooms[newUserName] = new HashSet<string>(_userRooms[name]);

                            if (_userRooms[name].Any())
                            {
                                foreach (var r in _userRooms[name])
                                {
                                    _rooms[r].Users.Remove(name);
                                    _rooms[r].Users.Add(newUserName);
                                    Clients.Group(r).changeUserName(oldUser, newUser);
                                }
                            }
                            HashSet<string> ignoredRoom;
                            ChatUser ignoredUser;
                            _userRooms.TryRemove(name, out ignoredRoom);
                            _users.TryRemove(name, out ignoredUser);

                            Clients.Caller.hash = newUser.Hash;
                            Clients.Caller.name = newUser.Name;

                            Clients.Caller.changeUserName(oldUser, newUser);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format("Username '{0}' is already taken!", newUserName));
                    }

                    return true;
                }
                else
                {
                    EnsureUser();
                    if (commandName.Equals("rooms", StringComparison.OrdinalIgnoreCase))
                    {
                        var rooms = _rooms.Select(r => new
                        {
                            Name = r.Key,
                            Count = r.Value.Users.Count
                        });

                        Clients.Caller.showRooms(rooms);

                        return true;
                    }
                    else if (commandName.Equals("join", StringComparison.OrdinalIgnoreCase))
                    {
                        if (parts.Length == 1)
                        {
                            throw new InvalidOperationException("Join which room?");
                        }

                        // Only support one room at a time for now

                        string newRoom = parts[1];
                        ChatRoom chatRoom;
                        // Create the room if it doesn't exist
                        if (!_rooms.TryGetValue(newRoom, out chatRoom))
                        {
                            chatRoom = new ChatRoom();
                            _rooms.TryAdd(newRoom, chatRoom);
                        }

                        // Remove the old room
                        if (!String.IsNullOrEmpty(room))
                        {
                            _userRooms[name].Remove(room);
                            _rooms[room].Users.Remove(name);

                            Clients.Group(room).leave(_users[name]);
                            Groups.Remove(Context.ConnectionId, room);
                        }

                        _userRooms[name].Add(newRoom);
                        if (!chatRoom.Users.Add(name))
                        {
                            throw new InvalidOperationException("You're already in that room!");
                        }

                        Clients.Group(newRoom).addUser(_users[name]);

                        // Set the room on the caller
                        Clients.Caller.room = newRoom;

                        Groups.Add(Context.ConnectionId, newRoom);

                        Clients.Caller.refreshRoom(newRoom);

                        return true;
                    }
                    else if (commandName.Equals("msg", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_users.Count == 1)
                        {
                            throw new InvalidOperationException("You're the only person in here...");
                        }

                        if (parts.Length < 2)
                        {
                            throw new InvalidOperationException("Who are you trying send a private message to?");
                        }

                        string to = parts[1];
                        if (to.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("You can't private message yourself!");
                        }

                        if (!_users.ContainsKey(to))
                        {
                            throw new InvalidOperationException(String.Format("Couldn't find any user named '{0}'.", to));
                        }

                        string messageText = String.Join(" ", parts.Skip(2)).Trim();

                        if (String.IsNullOrEmpty(messageText))
                        {
                            throw new InvalidOperationException(String.Format("What did you want to say to '{0}'.", to));
                        }

                        string recipientId = _users[to].ConnectionId;
                        // Send a message to the sender and the sendee                        
                        Clients.Group(recipientId).sendPrivateMessage(name, to, messageText);
                        Clients.Caller.sendPrivateMessage(name, to, messageText);

                        return true;
                    }
                    else
                    {
                        EnsureUserAndRoom();
                        if (commandName.Equals("me", StringComparison.OrdinalIgnoreCase))
                        {
                            if (parts.Length == 1)
                            {
                                throw new InvalidProgramException("You what?");
                            }
                            var content = String.Join(" ", parts.Skip(1));

                            Clients.Group(room).sendMeMessage(name, content);
                            return true;
                        }
                        else if (commandName.Equals("leave", StringComparison.OrdinalIgnoreCase))
                        {
                            ChatRoom chatRoom;
                            if (_rooms.TryGetValue(room, out chatRoom))
                            {
                                chatRoom.Users.Remove(name);
                                _userRooms[name].Remove(room);

                                Clients.Group(room).leave(_users[name]);
                            }

                            Groups.Remove(Context.ConnectionId, room);

                            Clients.Caller.room = null;

                            return true;
                        }

                        throw new InvalidOperationException(String.Format("'{0}' is not a valid command.", parts[0]));
                    }
                }
            }
            return false;
        }

        private ChatUser AddUser(string newUserName)
        {
            var user = new ChatUser(newUserName, GetMD5Hash(newUserName));
            user.ConnectionId = Context.ConnectionId;
            _users[newUserName] = user;
            _userRooms[newUserName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Clients.Caller.name = user.Name;
            Clients.Caller.hash = user.Hash;
            Clients.Caller.id = user.Id;

            Clients.Caller.addUser(user);

            return user;
        }

        private void EnsureUserAndRoom()
        {
            EnsureUser();

            // TODO: Restore when groups work
            string room = Clients.Caller.room;
            string name = Clients.Caller.name;

            if (String.IsNullOrEmpty(room) || !_rooms.ContainsKey(room))
            {
                throw new InvalidOperationException("Use '/join room' to join a room.");
            }

            HashSet<string> rooms;
            if (!_userRooms.TryGetValue(name, out rooms) || !rooms.Contains(room))
            {
                throw new InvalidOperationException(String.Format("You're not in '{0}'. Use '/join {0}' to join it.", room));
            }
        }

        private void EnsureUser()
        {
            string name = Clients.Caller.name;
            if (String.IsNullOrEmpty(name) || !_users.ContainsKey(name))
            {
                throw new InvalidOperationException("You don't have a name. Pick a name using '/nick nickname'.");
            }
        }

        private string Transform(string message, out HashSet<string> extractedUrls)
        {
            const string urlPattern = @"((https?|ftp)://|www\.)[\w]+(.[\w]+)([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])";

            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            message = Regex.Replace(message, urlPattern, m =>
            {
                string httpPortion = String.Empty;
                if (!m.Value.Contains("://"))
                {
                    httpPortion = "http://";
                }

                string url = httpPortion + m.Value;

                urls.Add(url);

                return String.Format(CultureInfo.InvariantCulture,
                                     "<a rel=\"nofollow external\" target=\"_blank\" href=\"{0}\" title=\"{1}\">{1}</a>",
                                     url, m.Value);
            });

            extractedUrls = urls;
            return message;
        }

        private Task<string> ExtractContent(string url)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            var requestTask = Task.Factory.FromAsync((cb, state) => request.BeginGetResponse(cb, state), ar => request.EndGetResponse(ar), null);
            return requestTask.ContinueWith(task => ExtractContent((HttpWebResponse)task.Result));
        }

        private string ExtractContent(HttpWebResponse response)
        {
            return _contentProviders.Select(c => c.GetContent(response))
                                    .FirstOrDefault(content => content != null);
        }

        [Serializable]
        public class ChatMessage
        {
            public string Id { get; private set; }
            public string User { get; set; }
            public string Text { get; set; }
            public ChatMessage(string user, string text)
            {
                User = user;
                Text = text;
                Id = Guid.NewGuid().ToString("d");
            }
        }

        [Serializable]
        public class ChatUser
        {
            public string ConnectionId { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Hash { get; set; }

            public ChatUser()
            {
            }

            public ChatUser(string name, string hash)
            {
                Name = name;
                Hash = hash;
                Id = Guid.NewGuid().ToString("d");
            }
        }

        public class ChatRoom
        {
            public List<ChatMessage> Messages { get; set; }
            public HashSet<string> Users { get; set; }

            public ChatRoom()
            {
                Messages = new List<ChatMessage>();
                Users = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}