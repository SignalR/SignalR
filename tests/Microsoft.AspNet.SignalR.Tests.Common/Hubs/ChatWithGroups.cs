using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    [HubName("groupChat")]
    public class ChatWithGroups : Hub
    {
        public void Send(string group, string message)
        {
            Clients.Group(group).send(message);
        }

        public async Task Join(string group)
        {
            await Groups.Add(Context.ConnectionId, group);
            await Clients.Caller.joinedGroup(group);
        }

        public void Leave(string group)
        {
            Groups.Remove(Context.ConnectionId, group);
        }

        public void TriggerAppDomainRestart()
        {
            HttpRuntime.UnloadAppDomain();
        }
    }
}
