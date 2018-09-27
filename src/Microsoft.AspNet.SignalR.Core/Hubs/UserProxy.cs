// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class UserProxy : SignalProxy
    {
        public UserProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubUserPrefix, ListHelper<string>.Empty)
        {

        }

        public UserProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, ITraceManager traceManager) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubUserPrefix, traceManager, ListHelper<string>.Empty)
        {

        }
    }
}
