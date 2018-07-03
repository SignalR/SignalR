// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class DelayedOnConnectedHub : Hub
    {
        public async override Task OnConnected()
        {
            await Task.Delay(5000);
            await base.OnConnected();
        }
    }
}
