using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class GroupJoiningHub : Hub
    {
        public static string GroupName = "Foo";

        public override Task OnConnected()
        {
            Groups.Add(Context.ConnectionId, GroupName).Wait();

            return PingGroup();
        }

        public Task PingGroup()
        {
            return Clients.Group(GroupName).ping();
        }
    }
}
