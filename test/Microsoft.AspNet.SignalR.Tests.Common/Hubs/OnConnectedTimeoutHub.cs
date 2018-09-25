using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class OnConnectedTimeoutHub : Hub
    {
        public override Task OnConnected()
        {
            // Never completes!
            var tcs = new TaskCompletionSource<object>();
            return tcs.Task;
        }
    }
}
