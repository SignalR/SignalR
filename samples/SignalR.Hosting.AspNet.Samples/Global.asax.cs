using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Routing;
using SignalR.Hubs;
using SignalR.Samples.Hubs.DemoHub;
using SignalR.Samples.Raw;
using SignalR.Samples.Streaming;

namespace SignalR.Hosting.AspNet.Samples
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            //GlobalHost.DependencyResolver.UseSqlServer(ConfigurationManager.ConnectionStrings["SignalRSamples"].ConnectionString);
            GlobalHost.HubPipeline.AddModule(new SamplePipelineModule());

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var context = GlobalHost.ConnectionManager.GetConnectionContext<Streaming>();
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<DemoHub>();

                while (true)
                {
                    try
                    {
                        context.Connection.Broadcast(DateTime.Now.ToString());
                        hubContext.Clients.fromArbitraryCode(DateTime.Now.ToString());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("SignalR error thrown in Streaming broadcast: {0}", ex);
                    }
                    Thread.Sleep(2000);
                }
            });

            RouteTable.Routes.MapHubs();

            RouteTable.Routes.MapConnection<SendingConnection>("sending-connection", "sending-connection/{*operation}");
            RouteTable.Routes.MapConnection<TestConnection>("test-connection", "test-connection/{*operation}");
            RouteTable.Routes.MapConnection<Raw>("raw", "raw/{*operation}");
            RouteTable.Routes.MapConnection<Streaming>("streaming", "streaming/{*operation}");
        }

        private class SamplePipelineModule : HubPipelineModule
        {
            protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
            {
                Debug.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
                return base.OnBeforeIncoming(context);
            }

            protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
            {
                Debug.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub); 
                return base.OnBeforeOutgoing(context);
            }
        }
    }
}