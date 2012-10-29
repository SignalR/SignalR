using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Samples;
using Microsoft.AspNet.SignalR.Samples.Raw;

namespace Microsoft.AspNet.SignalR.Hosting.Self.Samples
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
            server.HubPipeline.EnableAutoRejoiningGroups();

            // Map connections
            server.MapConnection<MyConnection>("/echo")
                  .MapConnection<RawConnection>("/raw")
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

            protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }

            protected override Task OnDisconnectAsync(IRequest request, string connectionId)
            {
                Console.WriteLine("{0} left", connectionId);
                return base.OnDisconnectAsync(request, connectionId);
            }

            protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
            {
                return groups;
            }
        }
    }
}
