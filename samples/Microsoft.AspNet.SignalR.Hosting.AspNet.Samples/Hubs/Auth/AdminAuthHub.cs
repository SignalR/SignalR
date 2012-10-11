using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [Authorize(Roles="Admin")]
    public class AdminAuthHub : NoAuthHub
    {
    }
}