// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class ConnectionIdProxy : SignalProxy
    {
        public ConnectionIdProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, ITraceManager traceManager, params string[] exclude) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubConnectionIdPrefix, traceManager, exclude)
        {
        }
            
        public ConnectionIdProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, params string[] exclude) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubConnectionIdPrefix, exclude)
        {

        }
    }
}
