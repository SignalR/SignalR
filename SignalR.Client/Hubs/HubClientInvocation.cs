using System.Collections.Generic;

namespace SignalR.Client.Hubs
{
    public class HubClientInvocation
    {
        public string Hub { get; set; }
        public string Method { get; set; }
        public object[] Args { get; set; }
        public Dictionary<string, object> State { get; set; }
    }
}
