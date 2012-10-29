using System;
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