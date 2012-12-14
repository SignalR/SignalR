using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class SyncErrorConnection : PersistentConnection
    {
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            throw new InvalidOperationException("This is a bug!");
        }
    }
}
