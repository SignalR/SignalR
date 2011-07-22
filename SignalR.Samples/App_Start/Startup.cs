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
            //var signalBus = new PeerToPeerHttpSignalBus();
            //var cs = ConfigurationManager.ConnectionStrings["SignalR"].ConnectionString;
            //var messageStore = new SQLMessageStore(cs);
            //DependencyResolver.Register(typeof(ISignalBus), () => signalBus);
            //DependencyResolver.Register(typeof(IMessageStore), () => messageStore);

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