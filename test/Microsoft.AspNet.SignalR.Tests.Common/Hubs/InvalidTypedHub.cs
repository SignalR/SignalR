// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class InvalidTypedHub : Hub<IInvalidClientContract>
    {
        public void Echo(string message)
        {
            Clients.Caller.Echo(message);
        }

        public async Task Ping()
        {
            await Clients.Caller.Ping();
        }
    }
}
