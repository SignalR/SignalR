using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubContext : IHubContext
    {
        private Func<string, ClientHubInvocation, IEnumerable<string>, Task> _send;
        private readonly string _hubName;
        private readonly IConnection _connection;

        public HubContext(Func<string, ClientHubInvocation, IEnumerable<string>, Task> send, string hubName, IConnection connection)
        {
            _send = send;
            _hubName = hubName;
            _connection = connection;

            Clients = new ClientProxy(send, hubName);
            Groups = new GroupManager(connection, _hubName);
        }

        public dynamic Clients { get; private set; }

        public IGroupManager Groups { get; private set; }

        public dynamic Group(string groupName, params string[] exclude)
        {
            return new SignalProxy(_send, groupName, _hubName, exclude);
        }

        public dynamic Client(string connectionId)
        {
            return new SignalProxy(_send, connectionId, _hubName);
        }

        public dynamic AllExcept(params string[] exclude)
        {
            // REVIEW: Should this method be params array?
            return new ClientProxy(_send, _hubName, exclude);
        }
    }
}
