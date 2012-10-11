using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [Authorize(Users="User")]
    [Authorize(Roles="Admin")]
    public class UserAndRoleAuthHub : NoAuthHub
    {
    }
}