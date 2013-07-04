using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class AutoEncodedJsonConnection : PersistentConnection
    {
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            return Connection.Send(connectionId, data);
        }
    }
}
