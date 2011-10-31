using System;
using SignalR.Hubs;

namespace SignalR.Samples.Hubs.ConnectDisconnect
{
    public class Status : Hub, IDisconnect
    {
        public void Join()
        {
            Clients.joined(Context.ClientId, DateTime.Now.ToString());
        }

        public void Disconnect()
        {
            Clients.leave(Context.ClientId, DateTime.Now.ToString());
        }
    }
}