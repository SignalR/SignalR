using SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Samples.Hubs.ConnectDisconnect
{
    [HubName("StatusHub")]
    public class Status : Hub
    {
        public override Task Disconnect()
        {
            return Clients.leave(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task Connect()
        {
            return Clients.joined(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task Reconnect(IEnumerable<string> groups)
        {
            return Clients.rejoined(Context.ConnectionId, DateTime.Now.ToString());
        }
    }
}