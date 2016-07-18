// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class UserProxy : SignalProxy
    {
        public UserProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName) :
            base(connection, invoker, signal, hubName, PrefixHelper.HubUserPrefix, ListHelper<string>.Empty)
        {

        }
    }
}
