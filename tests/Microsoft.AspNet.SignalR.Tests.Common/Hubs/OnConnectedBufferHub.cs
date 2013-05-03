using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class OnConnectedBufferHub : Hub
    {
        public static int bufferMeValues = 0;

        public override Task OnConnected()
        {
            Clients.Caller.bufferMe(Interlocked.Increment(ref bufferMeValues));
            Clients.Caller.bufferMe(Interlocked.Increment(ref bufferMeValues));

            Thread.Sleep(TimeSpan.FromSeconds(2));

            return base.OnConnected();
        }

        public void Ping()
        {
            Clients.Caller.pong();
        }
    }
}
