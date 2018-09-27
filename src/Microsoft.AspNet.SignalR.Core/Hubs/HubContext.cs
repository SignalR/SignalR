// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubContext : IHubContext<object>, IHubContext
    {
        public HubContext(IConnection connection, IHubPipelineInvoker invoker, string hubName, ITraceManager traceManager)
        {
            Clients = new HubConnectionContextBase(connection, invoker, hubName, traceManager);
            Groups = new GroupManager(connection, PrefixHelper.GetHubGroupName(hubName));
        }

        public IHubConnectionContext<dynamic> Clients { get; private set; }

        public IGroupManager Groups { get; private set; }
    }
}
