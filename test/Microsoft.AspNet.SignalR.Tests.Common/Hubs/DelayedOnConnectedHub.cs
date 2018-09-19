// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class DelayedOnConnectedHub : Hub
    {
        public override async Task OnConnected()
        {
            await Task.Delay(5000);
            await base.OnConnected();
        }
    }
}
