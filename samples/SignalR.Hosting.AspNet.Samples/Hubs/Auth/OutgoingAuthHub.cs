using SignalR.Hubs;

namespace SignalR.Samples.Hubs.Auth
{
    [Authorize(Mode=AuthorizeMode.Outgoing)]
    public class OutgoingAuthHub : NoAuthHub
    {
    }
}