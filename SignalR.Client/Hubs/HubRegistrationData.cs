using System.Collections.Generic;

namespace SignalR.Client.Hubs
{
    public class HubRegistrationData
    {
        public string Name { get; set; }
        public IEnumerable<string> Methods { get; set; }
    }
}
