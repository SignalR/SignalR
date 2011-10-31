using System.Collections.Generic;

namespace SignalR.Client.Hubs
{
    // Consolidate this and HubInvocation (they're the same just have slightly different property names)
    public class HubData
    {
        public string Hub { get; set; }
        public string Action { get; set; }
        public object[] Data { get; set; }
        public Dictionary<string, object> State { get; set; }
    }
}
