using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class ConnectionThatUsesItems : PersistentConnection
    {
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return PrintEnvironment("OnConnectedAsync", request, connectionId);
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            return PrintEnvironment("OnReceivedAsync", request, connectionId);
        }

        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            return PrintEnvironment("OnDisconnectAsync", request, connectionId);
        }

        private Task PrintEnvironment(string method, IRequest request, string connectionId)
        {
            return Connection.Broadcast(new
            {
                method = method,
                headers = request.Headers,
                query = request.QueryString,
                count = request.Environment.Count,
                owinKeys = request.Environment.Keys
            });
        }
    }
}
