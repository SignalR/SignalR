using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [Authorize(Mode=AuthorizeMode.Outgoing)]
    public class OutgoingAuthHub : NoAuthHub
    {
    }
}