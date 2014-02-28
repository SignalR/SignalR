using System.Diagnostics;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Redis;

namespace Microsoft.AspNet.SignalR.Samples
{
    public partial class Startup
    {
        public static void ConfigureSignalR(IDependencyResolver dependencyResolver, IHubPipeline hubPipeline)
        {
            // Uncomment the following line to enable scale-out using SQL Server
            //dependencyResolver.UseSqlServer(System.Configuration.ConfigurationManager.ConnectionStrings["SignalRSamples"].ConnectionString);

            // Uncomment the following line to enable scale-out using Redis
            //var config = new RedisScaleoutConfiguration("127.0.0.1", 6379, "", "SignalRSamples");
            //config.RetryOnError = true;
            //dependencyResolver.UseRedis(config);
            //dependencyResolver.UseRedis("127.0.0.1", 6379, "", "SignalRSamples");

            // Uncomment the following line to enable scale-out using service bus
            //dependencyResolver.UseServiceBus("connection string", "Microsoft.AspNet.SignalR.Samples");

            hubPipeline.AddModule(new SamplePipelineModule());
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