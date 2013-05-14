using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class MySendingConnection : PersistentConnection
    {
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            Connection.Send(connectionId, "OnConnectedAsync1");
            Connection.Send(connectionId, "OnConnectedAsync2");

            return base.OnConnected(request, connectionId);
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            Connection.Send(connectionId, "OnReceivedAsync1");
            Connection.Send(connectionId, "OnReceivedAsync2");

            return base.OnReceived(request, connectionId, data);
        }
    }
}
