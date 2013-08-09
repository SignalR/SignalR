using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class InvalidTypedHub : Hub<IInvalidClientContract>
    {
        public void Echo(string message)
        {
            Clients.Caller.Echo(message);
        }

        public async Task Ping()
        {
            await Clients.Caller.Ping();
        }
    }
}
