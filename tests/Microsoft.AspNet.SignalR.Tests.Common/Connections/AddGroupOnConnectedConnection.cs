using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    class AddGroupOnConnectedConnection : PersistentConnection
    {
        protected override async Task OnConnectedAsync(IRequest request, string connectionId)
        {
            await Groups.Add(connectionId, "test");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            await Groups.Add(connectionId, "test2");
        }

        protected override async Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            await Groups.Send("test2", "hey");
        }
    }
}
