using Microsoft.AspNet.SignalR.Tracing;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.Test
{
    public class StressHub : Hub
    {
        private readonly TraceSource _trace;

        public StressHub()
        {
            _trace = GlobalHost.DependencyResolver.Resolve<ITraceManager>()["SignalR.ScaleoutMessageBus"];
        }

        public override Task OnConnected()
        {
            _trace.TraceVerbose(typeof(StressHub).Name + ".OnConnected");
            Clients.All.clientConnected(new NodeEvent(Context.ConnectionId));
            return base.OnConnected();
        }

        public override Task OnDisconnected()
        {
            _trace.TraceVerbose(typeof(StressHub).Name + ".OnDisconnected");
            Clients.All.clientDisconnected(new NodeEvent(Context.ConnectionId));
            return base.OnDisconnected();
        }

        public override Task OnReconnected()
        {
            _trace.TraceVerbose(typeof(StressHub).Name + ".OnReconnected");
            Clients.All.clientReconnected(new NodeEvent(Context.ConnectionId));
            return base.OnReconnected();
        }

        public void ClientsCaller(int message)
        {
            Clients.Caller.clientsCaller(new NodeEvent(message));
        }

        public void ClientsGroup(string groupName, int message)
        {            
            Clients.Group(groupName).clientsGroup(new NodeEvent(message));
            Clients.Caller.clientsCallerGroup(new NodeEvent(message));
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

            Groups.Add(connectionId, groupName);
            Clients.Client(connectionId).joinedGroupsAdd(new NodeEvent(groupName));
        }
    }
}