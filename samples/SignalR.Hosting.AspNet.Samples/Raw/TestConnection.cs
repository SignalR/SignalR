using System.Threading.Tasks;

namespace SignalR.Hosting.AspNet.Samples
{
    public class TestConnection : PersistentConnection
    {
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            return Connection.Send(connectionId, data);
        }
    }
}