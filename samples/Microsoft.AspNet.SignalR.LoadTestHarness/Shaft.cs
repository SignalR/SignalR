using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class ShaftHub : Hub
    {
        public Task Echo(string data)
        {
            return Clients.Caller.invoke(data);
        }

        public Task Broadcast(string data)
        {
            return Clients.All.invoke(data);
        }
    }

    public class Shaft : PersistentConnection
    {
        internal static EndpointBehavior Behavior { get; set; }

        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            if (Behavior == EndpointBehavior.Echo)
            {
                Connection.Send(connectionId, data);
            }
            else if (Behavior == EndpointBehavior.Broadcast)
            {
                Connection.Broadcast(data);
            }
            return TaskAsyncHelper.Empty;
        }
    }

    public enum EndpointBehavior
    {
        ListenOnly,
        Echo,
        Broadcast
    }
}