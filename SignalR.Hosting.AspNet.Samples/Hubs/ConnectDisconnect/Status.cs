using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Hosting;
using SignalR.Hubs;

namespace SignalR.Samples.Hubs.ConnectDisconnect
{
    public class Status : Hub, IDisconnect, IConnected
    {
        public Task Disconnect()
        {
            return Clients.leave(Context.ConnectionId, DateTime.Now.ToString());
        }

        public Task Connect(IRequest request, IEnumerable<string> groups)
        {
            return Clients.joined(Context.ConnectionId, DateTime.Now.ToString());
        }

        public Task Reconnect(IRequest request, IEnumerable<string> groups)
        {
            return Clients.rejoined(Context.ConnectionId, DateTime.Now.ToString());
        }
    }
}