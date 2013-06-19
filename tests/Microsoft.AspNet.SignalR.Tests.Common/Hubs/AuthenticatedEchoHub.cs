using Microsoft.AspNet.SignalR.FunctionalTests.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    [Authorize]
    public class AuthenticatedEchoHub : EchoHub
    {
    }
}
