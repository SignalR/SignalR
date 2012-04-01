using System;
using System.Diagnostics;
using System.Threading;
using System.Web.Routing;
using SignalR.Hosting.AspNet.Routing;
using SignalR.Samples.Hubs.DemoHub;
using SignalR.Samples.Raw;
using SignalR.Samples.Streaming;

namespace SignalR.Hosting.AspNet.Samples
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var connection = SignalR.Global.Connections.GetConnection<Streaming>();
                var demoClients = SignalR.Global.Connections.GetClients<DemoHub>();

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

            RouteTable.Routes.MapConnection<Raw>("raw", "raw/{*operation}");
            RouteTable.Routes.MapConnection<Streaming>("streaming", "streaming/{*operation}");
        }
    }
}