using System;
using System.Diagnostics;
using System.Threading;
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

            // Uncomment this for web farm support
            //var cs = ConfigurationManager.ConnectionStrings["SignalR"].ConnectionString;
            //var store = new PeerToPeerSQLSignalBusMessageStore(cs);
            //DependencyResolver.Register(typeof(ISignalBus), () => store);
            //DependencyResolver.Register(typeof(IMessageStore), () => store);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var connection = Connection.GetConnection<Streaming.Streaming>();
                var demoClients = Hub.GetClients<DemoHub>();
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
        }
    }
}