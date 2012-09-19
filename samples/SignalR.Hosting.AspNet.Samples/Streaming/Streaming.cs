﻿using System.Collections.Generic;

namespace SignalR.Samples.Streaming
{
    public class Streaming : PersistentConnection
    {
        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return groups;
        }
    }
}