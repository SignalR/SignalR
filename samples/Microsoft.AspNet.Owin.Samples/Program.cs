using System;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Samples.Raw;
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
            // add SignalR to pipeline
            app.MapHubs("/signalr");

            app.MapConnection<RawConnection>("/raw");
        }
    }

    public class Chat : Hub
    {
        public void Send(string message)
        {
            Clients.All.send(message);
        }
    }

}
