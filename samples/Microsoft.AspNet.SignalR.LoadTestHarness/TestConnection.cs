using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public class TestConnection : PersistentConnection
    {
        internal static ConnectionBehavior Behavior { get; set; }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            if (Behavior == ConnectionBehavior.Echo)
            {
                Connection.Send(connectionId, data);
            }
            else if (Behavior == ConnectionBehavior.Broadcast)
            {
                Connection.Broadcast(data);
            }
            return TaskAsyncHelper.Empty;
        }
    }

    public enum ConnectionBehavior
    {
        ListenOnly,
        Echo,
        Broadcast
    }
}