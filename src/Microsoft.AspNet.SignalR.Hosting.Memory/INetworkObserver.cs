using System;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{
    public interface INetworkObserver
    {
        Action OnCancel { get; set; }
        Action OnClose { get; set; }
        Action<ArraySegment<byte>> OnWrite { get; set; }
    }
}
