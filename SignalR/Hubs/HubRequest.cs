using System.Collections.Generic;

namespace SignalR.Hubs
{
    public class HubRequest
    {
        public string Hub { get; set; }
        public string Method { get; set; }
        public IParameterValue[] ParameterValues { get; set; }
        public IDictionary<string, object> State { get; set; }
        public string Id { get; set; }
    }
}
