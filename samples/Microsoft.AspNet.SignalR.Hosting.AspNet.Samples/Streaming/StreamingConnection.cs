﻿using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Samples.Streaming
{
    public class StreamingConnection : PersistentConnection
    {
        protected override System.Threading.Tasks.Task OnConnected(IRequest request, string connectionId)
        {
            return base.OnConnected(request, connectionId);
        }

        protected override IList<string> OnRejoiningGroups(IRequest request, IList<string> groups, string connectionId)
        {
            return groups;
        }

        protected override System.Threading.Tasks.Task OnDisconnected(IRequest request, string connectionId)
        {
            return base.OnDisconnected(request, connectionId);
        }
    }
}