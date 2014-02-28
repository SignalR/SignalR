using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class FallbackToLongPollingConnectionThrows : PersistentConnection
    {
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            throw new InvalidOperationException();
        }
    }
}
