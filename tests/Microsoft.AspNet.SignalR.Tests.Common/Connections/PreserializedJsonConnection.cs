using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class PreserializedJsonConnection : PersistentConnection
    {
        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            var jsonBytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
            return Connection.Send(connectionId, jsonBytes);
        }
    }
}
