using System;
using System.Collections.Generic;

namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public class AutoRejoiningGroupsModule : HubPipelineModule
    {
        public override Func<IHub, IEnumerable<string>, IEnumerable<string>> BuildRejoiningGroups(Func<IHub, IEnumerable<string>, IEnumerable<string>> rejoiningGroups)
        {
            return (hub, groups) => groups;
        }
    }
}
