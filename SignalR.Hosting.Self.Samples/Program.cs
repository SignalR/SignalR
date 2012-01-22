using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SignalR.Hosting.Self;

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

            // Enable the hubs route (/signalr)
            server.EnableHubs();

            server.Start();

            Console.WriteLine("Server running on {0}", url);

            Console.ReadKey();
        }

        public class MyConnection : PersistentConnection
        {
            protected override Task OnReceivedAsync(string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }
    }
}
