// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [AuthorizeClaims]
    public class HeaderAuthHub : Hub
    {
        public override Task OnConnected()
        {
            return Clients.Caller.display("Authenticated and Conencted!");
        }
    }
}
