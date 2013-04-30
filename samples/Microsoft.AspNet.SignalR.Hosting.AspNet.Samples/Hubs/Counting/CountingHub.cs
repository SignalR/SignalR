using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Counting
{
    public class CountingHub : Hub
    {
        public async Task Send(int n)
        {
            await Clients.All.send(n);
        }
    }
}