using System.Threading;
using System.Web;

namespace SignalR.Samples.Performance
{
    /// <summary>
    /// Summary description for Performance
    /// </summary>
    public class Performance : PersistentConnection
    {
        private static int _connectedClients;

        protected override void OnConnected(HttpContextBase context, string connectionId)
        {
            Interlocked.Increment(ref _connectedClients);
        }

        protected override void OnReceived(string connectionId, string data)
        {
            if (data == "reset")
            {
                _connectedClients = 0;
            }
            else
            {
                Connection.Broadcast(new { data = data, clients = _connectedClients });
            }
        }

        protected override void OnDisconnect(string connectionId)
        {
            Interlocked.Decrement(ref _connectedClients);
        }
    }
}