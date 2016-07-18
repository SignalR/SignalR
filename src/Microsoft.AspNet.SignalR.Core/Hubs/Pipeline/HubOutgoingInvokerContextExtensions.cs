// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal static class HubOutgoingInvokerContextExtensions
    {
        public static ConnectionMessage GetConnectionMessage(this IHubOutgoingInvokerContext context)
        {
            if (String.IsNullOrEmpty(context.Signal))
            {
                return new ConnectionMessage(context.Signals, context.Invocation, context.ExcludedSignals);
            }
            else
            {
                return new ConnectionMessage(context.Signal, context.Invocation, context.ExcludedSignals);
            }
        }
    }
}
