using System.Diagnostics;

namespace SignalR.Client
{
    [DebuggerDisplay("{ClientId} {Url}")]
    public class NegotiationResponse
    {
        public string ClientId { get; set; }
        public string Url { get; set; }
    }
}
