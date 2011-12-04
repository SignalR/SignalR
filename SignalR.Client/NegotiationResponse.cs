using System.Diagnostics;

namespace SignalR.Client
{
    [DebuggerDisplay("{ConnectionId} {Url}")]
    public class NegotiationResponse
    {
        public string ConnectionId { get; set; }
        public string Url { get; set; }
    }
}
