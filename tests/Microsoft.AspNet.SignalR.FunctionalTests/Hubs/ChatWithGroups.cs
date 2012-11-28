using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Hubs
{
    [HubName("groupChat")]
    public class ChatWithGroups : Hub
    {
        public void Send(string group, string message)
        {
            Clients.Group(group).send(message);
        }

        public void Join(string group)
        {
            Groups.Add(Context.ConnectionId, group);
        }

        public void Leave(string group)
        {
            Groups.Remove(Context.ConnectionId, group);
        }
    }
}
