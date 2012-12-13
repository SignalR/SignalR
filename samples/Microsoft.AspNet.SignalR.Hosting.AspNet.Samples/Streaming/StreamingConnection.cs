using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Samples.Streaming
{
    public class StreamingConnection : PersistentConnection
    {
        protected override System.Threading.Tasks.Task OnConnectedAsync(IRequest request, string connectionId)
        {
            return base.OnConnectedAsync(request, connectionId);
        }

        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return groups;
        }

        protected override System.Threading.Tasks.Task OnDisconnectAsync(IRequest request, string connectionId)
        {
            return base.OnDisconnectAsync(request, connectionId);
        }
    }
}