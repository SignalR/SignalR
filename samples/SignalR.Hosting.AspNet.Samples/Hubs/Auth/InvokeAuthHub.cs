using System;
using SignalR.Hubs;

namespace SignalR.Samples.Hubs.Auth
{
    public class InvokeAuthHub : Hub
    {
        [Authorize(Roles="Admin,Invoker")]
        public void InvokedFromClient()
        {
            Clients.All.invoked(Context.ConnectionId, DateTime.Now.ToString());
        }
    }
}