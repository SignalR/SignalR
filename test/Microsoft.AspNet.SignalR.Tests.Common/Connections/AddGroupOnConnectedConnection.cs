// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    class AddGroupOnConnectedConnection : PersistentConnection
    {
        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            await Groups.Add(connectionId, "test");
            await Task.Delay(TimeSpan.FromSeconds(1));
            await Groups.Add(connectionId, "test2");
        }

        protected override async Task OnReceived(IRequest request, string connectionId, string data)
        {
            await Groups.Send("test2", "hey");
        }
    }
}
