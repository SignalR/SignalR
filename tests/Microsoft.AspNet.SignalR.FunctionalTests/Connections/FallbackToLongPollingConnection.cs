using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class FallbackToLongPollingConnection : PersistentConnection
    {
        protected override async Task OnConnectedAsync(IRequest request, string connectionId)
        {
            string transport = request.QueryString["transport"];

            if (transport != "longPolling")
            {
                await Task.Delay(5000);
            }

            await base.OnConnectedAsync(request, connectionId);
        }
    }
}
