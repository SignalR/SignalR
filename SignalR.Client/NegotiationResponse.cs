using System.Diagnostics;

namespace SignalR.Client
{
    [DebuggerDisplay("{ConnectionId} {Url} -> {ProtocolVersion}")]
    public class NegotiationResponse
    {
        public string ConnectionId { get; set; }
        public string Url { get; set; }
        public string ProtocolVersion { get; set; }
    }
}
