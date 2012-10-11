using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public class HubInvocation
    {
        public string Hub { get; set; }
        public string Method { get; set; }
        public JToken[] Args { get; set; }
        public Dictionary<string, JToken> State { get; set; }
    }
}
