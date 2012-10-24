using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class TestHub : Hub
    {
        public Task Echo(string data)
        {
            return Clients.Caller.invoke(data);
        }

        public Task Broadcast(string data)
        {
            return Clients.All.invoke(data);
        }
    }
}