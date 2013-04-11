using Microsoft.AspNet.SignalR.Tracing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.Test
{
    public class TestHub : Hub
    {
        static ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();
        static ConcurrentDictionary<string, string> _groups = new ConcurrentDictionary<string, string>();

        private readonly TraceSource _trace;

        public TestHub()
        {
            _trace = GlobalHost.DependencyResolver.Resolve<ITraceManager>()["SignalR.ScaleoutMessageBus"];
        }

        public override Task OnConnected()
        {
            _trace.TraceVerbose(typeof(TestHub).Name + ".OnConnected");
            _connections.GetOrAdd(Context.ConnectionId, string.Empty);
            Clients.All.clientConnected(new NodeEvent(Context.ConnectionId));
            Clients.Caller.groupsAll(new NodeEvent(_groups.Keys));
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            _trace.TraceVerbose(typeof(TestHub).Name + ".OnDisconnected");
            string value;
            _connections.TryRemove(Context.ConnectionId, out value);
            Clients.All.clientDisconnected(new NodeEvent(Context.ConnectionId));
            return base.OnDisconnected();
        }

        public override Task OnReconnected()
        {
            _trace.TraceVerbose(typeof(TestHub).Name + ".OnReconnected");
            Clients.All.clientReconnected(new NodeEvent(Context.ConnectionId));
            return base.OnReconnected();
        }

        public void ClientsAll(string message)
        {
            string connectionId = Context.ConnectionId;
            string data = string.Format("[{0}] {1}", connectionId, message);
            Clients.All.received(new NodeEvent(data));
        }

        public void ClientsCaller(string message)
        {
            string connectionId = Context.ConnectionId;
            string data = string.Format("[{0}] {1}", connectionId, message);
            Clients.Caller.received(new NodeEvent(data));
        }

        public void ClientsClient(string targetConnectionId, string message)
        {
            string connectionId = Context.ConnectionId;
            string data = string.Format("[{0}] {1}", connectionId, message);
            Clients.Client(targetConnectionId).received(new NodeEvent(data));
        }

        public void ClientsGroup(string groupName, string message)
        {
            string connectionId = Context.ConnectionId;
            string data = string.Format("[{0}, {1}] {2}", connectionId, groupName, message);
            Clients.Group(groupName).received(new NodeEvent(data));
        }

        public void GroupsAdd(string connectionId, string groupName)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                connectionId = Context.ConnectionId;
            }

            if (string.IsNullOrEmpty(groupName))
            {
                return;
            }

            if (!_groups.ContainsKey(groupName))
            {
                _groups.GetOrAdd(groupName, string.Empty);
                Clients.All.groupsAdd(new NodeEvent(groupName));
            }

            Groups.Add(connectionId, groupName);
            Clients.Client(connectionId).joinedGroupsAdd(new NodeEvent(groupName));
        }

        public void GroupsRemove(string connectionId, string groupName)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                connectionId = Context.ConnectionId;
            }

            if (string.IsNullOrEmpty(groupName))
            {
                return;
            }

            Groups.Remove(connectionId, groupName);
            Clients.Client(connectionId).joinedGroupsRemove(new NodeEvent(groupName));
        }
    }
}