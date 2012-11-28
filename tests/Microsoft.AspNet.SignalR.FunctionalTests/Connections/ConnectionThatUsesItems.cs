using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class ConnectionThatUsesItems : PersistentConnection
    {
        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return PrintEnvironment("OnConnectedAsync", request, connectionId);
        }

        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            return PrintEnvironment("OnReceivedAsync", request, connectionId);
        }

        protected override Task OnDisconnectAsync(IRequest request, string connectionId)
        {
            return PrintEnvironment("OnDisconnectAsync", request, connectionId);
        }

        private Task PrintEnvironment(string method, IRequest request, string connectionId)
        {
            object owinEnv;
            if (request.Items.TryGetValue("owin.environment", out owinEnv))
            {
                var env = (IDictionary<string, object>)owinEnv;
                return Connection.Broadcast(new
                {
                    method = method,
                    count = env.Count,
                    owinKeys = env.Keys,
                    keys = request.Items.Keys
                });
            }

            return Connection.Broadcast(new
            {
                method = method,
                count = 0,
                keys = new string[0],
                owinKeys = new string[0],
            });
        }
    }
}
