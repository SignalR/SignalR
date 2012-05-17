using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hubs;

namespace SignalR.Samples.Hubs.Benchmark
{
    public class HubBench : Hub, IConnected, IDisconnect
    {
        public static int Connections;

        public void HitMe(long start, int clientCalls, string connectionId)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < clientCalls; i++)
            {
                tasks.Add(Clients[connectionId].stepOne());
            }

            Task.WaitAll(tasks.ToArray());

            Clients[connectionId].doneOne(start, clientCalls).Wait();
        }

        public void HitUs(long start, int clientCalls)
        {
            for (int i = 0; i < clientCalls; i++)
            {
                Clients.stepAll().Wait();
            }

            Clients.doneAll(start, clientCalls, Connections, Context.ConnectionId).Wait();
        }

        public Task Connect()
        {
            Interlocked.Increment(ref HubBench.Connections);
            return null;
        }

        public Task Disconnect()
        {
            Interlocked.Decrement(ref HubBench.Connections);
            return null;
        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            return null;
        }
    }
}