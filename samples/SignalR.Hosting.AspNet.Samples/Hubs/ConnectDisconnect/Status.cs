using SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Samples.Hubs.ConnectDisconnect
{
    [HubName("StatusHub")]
    public class Status : Hub, IDisconnect, IConnected
    {
        public Task Disconnect()
        {
            return Clients.leave(Context.ConnectionId, DateTime.Now.ToString());
        }

        public Task Connect()
        {
            return Clients.joined(Context.ConnectionId, DateTime.Now.ToString());
        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            return Clients.rejoined(Context.ConnectionId, DateTime.Now.ToString());
        }

        [HubMethodName("John")]
        public void Foo()
        {
        }
    }
}