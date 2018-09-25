using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.ServiceSample
{
    public class EchoHub : Hub
    {
        public override Task OnConnected()
        {
            return Clients.All.receive("<system>", "User joined");
        }

        public Task Broadcast(string user, string message)
        {
            return Clients.All.receive(user, message);
        }
    }
}
