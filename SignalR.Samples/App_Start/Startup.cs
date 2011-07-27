using System;
using System.Configuration;
using System.Threading;
using SignalR.Infrastructure;
using SignalR.Samples.App_Start;
using SignalR.SignalBuses;

[assembly: WebActivator.PreApplicationStartMethod(typeof(Startup), "Start")]

namespace SignalR.Samples.App_Start {
    public class Startup {
        public static void Start() {
            
            // Uncomment this for web farm support
            //var cs = ConfigurationManager.ConnectionStrings["SignalR"].ConnectionString;
            //var store = new PeerToPeerSQLSignalBusMessageStore(cs);
            //DependencyResolver.Register(typeof(ISignalBus), () => store);
            //DependencyResolver.Register(typeof(IMessageStore), () => store);

            ThreadPool.QueueUserWorkItem(_ => {
                var connection = Connection.GetConnection<Streaming.Streaming>();

                while (true) {
                    connection.Broadcast(DateTime.Now.ToString());
                    Thread.Sleep(2000);
                }
            });
        }
    }
}