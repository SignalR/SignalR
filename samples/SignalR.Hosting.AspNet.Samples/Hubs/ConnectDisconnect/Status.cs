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
            return Clients.All.leave(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task Connect()
        {
            return Clients.All.joined(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task Reconnect()
        {
            return Clients.All.rejoined(Context.ConnectionId, DateTime.Now.ToString());
        }
    }
}