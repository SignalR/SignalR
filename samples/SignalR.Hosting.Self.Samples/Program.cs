using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SignalR.Samples.Raw;

namespace SignalR.Hosting.Self.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());
            Debug.AutoFlush = true;

            string url = "http://*:8081/";
            var server = new Server(url);
            server.Configuration.DisconnectTimeout = TimeSpan.Zero;

            // Map connections
            server.MapConnection<MyConnection>("/echo")
                  .MapConnection<Raw>("/raw")
                  .MapHubs();

            server.Start();

            Console.WriteLine("Server running on {0}", url);
            
            while (true)
            {
                ConsoleKeyInfo ki = Console.ReadKey(true);
                if (ki.Key == ConsoleKey.X)
                {
                    break;
                }
            }
        }

        public class MyConnection : PersistentConnection
        {
            protected override Task OnConnectedAsync(IRequest request, string connectionId)
            {
                Console.WriteLine("{0} connected", connectionId);
                return base.OnConnectedAsync(request, connectionId);
            }

            protected override Task OnReceivedAsync(string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }

            protected override Task OnDisconnectAsync(string connectionId)
            {
                Console.WriteLine("{0} left", connectionId);
                return base.OnDisconnectAsync(connectionId);
            }
        }
    }
}
