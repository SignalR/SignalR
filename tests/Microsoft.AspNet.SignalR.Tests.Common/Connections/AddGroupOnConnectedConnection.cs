using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    class AddGroupOnConnectedConnection : PersistentConnection
    {
        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            await Groups.Add(connectionId, "test");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            await Groups.Add(connectionId, "test2");
        }

        protected override async Task OnReceived(IRequest request, string connectionId, string data)
        {
            await Groups.Send("test2", "hey");
        }
    }
}
