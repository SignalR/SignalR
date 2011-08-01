using System.Collections.Generic;

namespace SignalR.Client.Hubs {
    public class HubRequest {
        public IEnumerable<string> Actions { get; set; }
    }
}
