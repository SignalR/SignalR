// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class AsyncOnConnectedConnection : PersistentConnection
    {
        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
