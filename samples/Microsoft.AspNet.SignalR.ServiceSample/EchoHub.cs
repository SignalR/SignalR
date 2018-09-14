using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.ServiceSample
{
    public class EchoHub : Hub
    {
        public string SayHi(string name)
        {
            return $"Hello, {name}!";
        }

        public async Task Broadcast(string message)
        {
            await Clients.All.receive(message);
        }
    }
}
