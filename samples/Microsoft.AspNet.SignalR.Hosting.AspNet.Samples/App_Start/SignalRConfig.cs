using System.Diagnostics;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Knockout;
using Microsoft.AspNet.SignalR.Samples.Hubs.KnockR;

namespace Microsoft.AspNet.SignalR.Samples
{
    public static class SignalRConfig
    {
        private static object _initialListHubState = new
        {
            itemToAdd = new Observable<string>("Your the first here! Add something."),
            items = new ObservableArray<string>(new[] {"New", "List", "Values" })
        };
            
        public static void ConfigureSignalR(IDependencyResolver dependencyResolver, IHubPipeline hubPipeline)
        {
            // Uncomment the following line to enable scale-out using SQL Server
            // dependencyResolver.UseSqlServer(ConfigurationManager.ConnectionStrings["SignalRSamples"].ConnectionString);

            // Uncomment the following line to enable scale-out using Redis 
            // dependencyResolver.UseRedis("127.0.0.1", 6379, "", new[] { "SignalRSamples" }); 

            dependencyResolver.ActivateKnockoutHub<ListHub>(_initialListHubState);

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