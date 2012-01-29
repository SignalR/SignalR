using System;
using System.Diagnostics;
using System.Threading;
using System.Web.Routing;
using SignalR.Hosting.AspNet;
using SignalR.Hosting.AspNet.Routing;
using SignalR.Hubs;
using SignalR.Samples.App_Start;
using SignalR.Samples.Hubs.DemoHub;

[assembly: WebActivator.PreApplicationStartMethod(typeof(Startup), "Start")]

namespace SignalR.Samples.App_Start
{
    public class Startup
    {
        public static void Start()
        { 
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var connection = Connection.GetConnection<Streaming.Streaming>(AspNetBootstrapper.DependencyResolver);
                var demoClients = Hub.GetClients<DemoHub>(AspNetBootstrapper.DependencyResolver);
                while (true)
                {
                    try
                    {
                        connection.Broadcast(DateTime.Now.ToString());
                        demoClients.fromArbitraryCode(DateTime.Now.ToString());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("SignalR error thrown in Streaming broadcast: {0}", ex);
                    }
                    Thread.Sleep(2000);
                }
            });

            RouteTable.Routes.MapConnection<Raw.Raw>("raw", "raw/{*operation}");
            RouteTable.Routes.MapConnection<Streaming.Streaming>("streaming", "streaming/{*operation}");
        }
    }
}