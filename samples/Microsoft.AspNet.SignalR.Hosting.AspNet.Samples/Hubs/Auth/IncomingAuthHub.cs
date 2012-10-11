using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [Authorize(Mode=AuthorizeMode.Incoming)]
    public class IncomingAuthHub : NoAuthHub
    {
    }
}