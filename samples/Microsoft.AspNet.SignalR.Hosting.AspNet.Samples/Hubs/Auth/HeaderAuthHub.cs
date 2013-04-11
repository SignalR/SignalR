using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [AuthorizeClaims]
    public class HeaderAuthHub : Hub
    {
        public override Task OnConnected()
        {
            return Clients.Caller.display("Authenticated and Conencted!");
        }
    }
}