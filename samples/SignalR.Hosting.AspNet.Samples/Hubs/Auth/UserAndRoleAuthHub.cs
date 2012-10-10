using SignalR.Hubs;

namespace SignalR.Samples.Hubs.Auth
{
    [Authorize(Users="User")]
    [Authorize(Roles="Admin")]
    public class UserAndRoleAuthHub : NoAuthHub
    {
    }
}