using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [Authorize(RequireOutgoing=false)]
    public class IncomingAuthHub : NoAuthHub
    {
    }
}