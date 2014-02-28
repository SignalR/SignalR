using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class GroupJoiningHub : Hub
    {
        public override Task OnConnected()
        {
            Groups.Add(Context.ConnectionId, Context.ConnectionId).Wait();

            return PingGroup();
        }

        public Task PingGroup()
        {
            return Clients.Group(Context.ConnectionId).ping();
        }
    }
}
