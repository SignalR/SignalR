using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    [HubName("ClientCallbackHub")]
    public class ClientCallbackHub : Hub
    {
        public Task SendInvalidNumberOfArguments()
        {
            return Clients.Caller.twoArgsMethod("arg1");
        }

        public Task SendArgumentsTypeMismatch()
        {
            return Clients.Caller.foo("arg1");
        }
    }
}
