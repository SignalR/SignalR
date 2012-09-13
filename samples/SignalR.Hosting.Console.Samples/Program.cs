using System.Collections.Generic;
using Microsoft.HttpListener.Owin;
using Owin;
using Owin.Builder;

namespace SignalR.Hosting.Console.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new AppBuilder();
            app.Properties["host.Addresses"] = HostAddresses;

            ServerFactory.Initialize(app);
            Startup.Configuration(app);
            var server = ServerFactory.Create(app.Build(), app.Properties);

            System.Console.WriteLine("Running on http://localhost:8080/");
            System.Console.WriteLine("Press enter to exit");
            System.Console.ReadLine();

            server.Dispose();
        }

        private static readonly List<IDictionary<string, object>> HostAddresses =
            new List<IDictionary<string, object>>
                {
                    new Dictionary<string, object>
                        {
                            {"scheme", "http"},
                            {"host", "+"},
                            {"port", "8080"},
                        },
                    new Dictionary<string, object>
                        {
                            {"scheme", "http"},
                            {"host", "+"},
                            {"port", "8081"},
                        },
                };
    }
}
