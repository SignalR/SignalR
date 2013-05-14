using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class FilteredConnection : PersistentConnection
    {
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            return Connection.Broadcast(data, connectionId);
        }
    }
}
