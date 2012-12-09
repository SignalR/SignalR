using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;
using Owin;

namespace Microsoft.AspNet.Owin.Samples
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (WebApplication.Start<Program>("http://localhost:8080/"))
            {
                Console.WriteLine("Server running at http://localhost:8080/");
                Console.ReadLine();
            }
        }

        public void Configuration(IAppBuilder app)
        {
            // Map hubs
            app.MapHubs("/signalr");

            // Map a connection
            app.MapConnection<Echo>("/echo");
        }
    }

    public class Chat : Hub
    {
        public void Send(string message)
        {
            Clients.All.send(message);
        }
    }

    public class Echo : PersistentConnection
    {
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            return Connection.Broadcast(data);
        }
    }
}
