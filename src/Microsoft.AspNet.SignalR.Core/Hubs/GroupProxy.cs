// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class GroupProxy : SignalProxy
    {
        public GroupProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, IList<string> exclude) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubGroupPrefix, exclude)
        {

        }

        public GroupProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, IList<string> exclude, ITraceManager traceManager) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubGroupPrefix, traceManager, exclude)
        {

        }
    }
}
