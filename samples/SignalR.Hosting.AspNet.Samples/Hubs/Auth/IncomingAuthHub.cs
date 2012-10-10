using SignalR.Hubs;

namespace SignalR.Samples.Hubs.Auth
{
    [Authorize(Mode=AuthorizeMode.Incoming)]
    public class IncomingAuthHub : NoAuthHub
    {
    }
}