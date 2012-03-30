using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SignalR.Hosting.Self.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());
            Debug.AutoFlush = true;

            string url = "http://localhost:8081/";
            var server = new Server(url);

            // Map /echo to the persistent connection
            server.MapConnection<MyConnection>("/echo");

            server.Start();

            Console.WriteLine("Server running on {0}", url);

            Console.ReadKey();
        }

        public class MyConnection : PersistentConnection
        {
            protected override Task OnConnectedAsync(IRequest request, string connectionId)
            {
                return Connection.Broadcast(String.Format("{0} connected from {1}", connectionId, request.Headers["User-Agent"]));
            }

            protected override Task OnReceivedAsync(string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }
    }
}
