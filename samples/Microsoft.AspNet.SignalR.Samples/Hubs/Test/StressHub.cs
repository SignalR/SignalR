using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.Test
{
    public class StressHub : Hub
    {
        private static TraceSource _trace = GlobalHost.DependencyResolver.Resolve<ITraceManager>()["SignalR.ScaleoutMessageBus"];

        public override Task OnConnected()
        {
            _trace.TraceVerbose(typeof(StressHub).Name + ".OnConnected");
            return Clients.All.clientConnected(new NodeEvent(Context.ConnectionId));
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _trace.TraceVerbose(typeof(StressHub).Name + ".OnDisconnected");
            return Clients.All.clientDisconnected(new NodeEvent(Context.ConnectionId));
        }

        public override Task OnReconnected()
        {
            _trace.TraceVerbose(typeof(StressHub).Name + ".OnReconnected");
            return Clients.All.clientReconnected(new NodeEvent(Context.ConnectionId));
        }

        public void EchoToCaller(int message)
        {
            Clients.Caller.receivedCaller(new NodeEvent(message));
        }

        public void EchoToGroup(string groupName, int message)
        {            
            Clients.Group(groupName).receivedGroup(new NodeEvent(message));
            Clients.Caller.receivedCaller(new NodeEvent(message));
        }

        public void JoinGroup(string groupName, string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                connectionId = Context.ConnectionId;
            }

            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentNullException("groupName");
            }

            Groups.Add(connectionId, groupName);
            Clients.Client(connectionId).joinedGroup(new NodeEvent(groupName));
        }
    }
}