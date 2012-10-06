using SignalR.Hubs;

namespace SignalR.Samples.Hubs.Auth
{
    [Authorize(Roles="Admin")]
    public class AdminAuthHub : NoAuthHub
    {
    }
}