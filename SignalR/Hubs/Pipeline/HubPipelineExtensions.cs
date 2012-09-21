using System;
using System.Collections.Generic;

namespace SignalR.Hubs
{
    public static class HubPipelineExtensions
    {
        public static void EnableAutoRejoiningGroups(this IHubPipeline pipeline)
        {
            pipeline.AddModule(new AutoRejoiningGroupsModule());
        }
    }
}
