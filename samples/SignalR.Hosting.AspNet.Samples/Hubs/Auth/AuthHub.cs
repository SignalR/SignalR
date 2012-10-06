using SignalR.Hubs;

namespace SignalR.Samples.Hubs.Auth
{
    [Authorize]
    public class AuthHub : NoAuthHub
    {
    }
}