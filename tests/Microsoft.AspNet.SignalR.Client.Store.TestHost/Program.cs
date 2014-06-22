using System;
using System.Threading;
using Microsoft.Owin.Hosting;
using Owin;

namespace Microsoft.AspNet.SignalR.Client.Store.TestHost
{
    // Used for running End-to-End tests for Store WebSockets transport.
    public class Program
    {
        static void Main(string[] args)
        {
            const string url = "http://localhost:42424";
            using (WebApp.Start(url))
            {
                Console.WriteLine("SignalR host for E2E Store Client tests running on {0}", url);
                Thread.Sleep(args.Length > 0 ? int.Parse(args[0]) : Timeout.Infinite);
            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
