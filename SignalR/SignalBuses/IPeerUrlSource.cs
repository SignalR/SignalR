using System.Collections.Generic;

namespace SignalR.SignalBuses {
    public interface IPeerUrlSource {
        IEnumerable<string> GetPeerUrls();
    }
}