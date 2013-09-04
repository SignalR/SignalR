using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub;
using Microsoft.AspNet.SignalR.Samples.Streaming;

namespace Microsoft.AspNet.SignalR.Samples
{
    public static class BackgroundThread
    {
        public static void Start()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var context = GlobalHost.ConnectionManager.GetConnectionContext<StreamingConnection>();
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<DemoHub>();

                context.Connection.Receive(async message =>
                {
                    Debug.WriteLine("Received a message on the streaming connection: {0}", (object)message);
                });

                hubContext.Connection.Receive(async message =>
                {
                    Debug.WriteLine("Received hub invocation: {0}", message);
                });

                hubContext.Subscribe(async invocation =>
                {
                    Debug.WriteLine("Invoked method {0} on {1}", invocation.Method, invocation.Hub);
                });

                while (true)
                {
                    try
                    {
                        context.Connection.Broadcast(DateTime.Now.ToString());
                        hubContext.Clients.All.fromArbitraryCode(DateTime.Now.ToString());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("SignalR error thrown in Streaming broadcast: {0}", ex);
                    }
                    Thread.Sleep(2000);
                }
            });
        }
    }
}