using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class FallbackToLongPollingConnection : PersistentConnection
    {
        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            string transport = request.QueryString["transport"];

            if (transport != "longPolling")
            {
                await Task.Delay(5000);
            }

            await base.OnConnected(request, connectionId);
        }
    }
}
