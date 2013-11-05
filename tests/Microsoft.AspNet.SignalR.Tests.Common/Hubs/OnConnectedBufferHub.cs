using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class OnConnectedBufferHub : Hub
    {
        public override Task OnConnected()
        {
            Clients.Caller.bufferMe(0);
            Clients.Caller.bufferMe(1);

            Thread.Sleep(TimeSpan.FromSeconds(2));

            return base.OnConnected();
        }

        public void Ping()
        {
            Clients.Caller.pong();
        }
    }
}