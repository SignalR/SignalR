using System.Collections.Generic;

namespace SignalR.ScaleOut
{
    public interface IPeerUrlSource
    {
        IEnumerable<string> GetPeerUrls();
    }
}