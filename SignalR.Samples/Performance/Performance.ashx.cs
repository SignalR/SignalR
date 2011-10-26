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

        protected override void OnConnected(HttpContextBase context, string clientId)
        {
            Interlocked.Increment(ref _connectedClients);
        }

        protected override void OnReceived(string clientId, string data)
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

        protected override void OnDisconnect(string clientId)
        {
            Interlocked.Decrement(ref _connectedClients);
        }
    }
}