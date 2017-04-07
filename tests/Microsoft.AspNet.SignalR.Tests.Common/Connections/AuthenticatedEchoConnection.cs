﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Connections
{
    public class AuthenticatedEchoConnection : PersistentConnection
    {
        protected override bool AuthorizeRequest(IRequest request)
        {
            return request.User != null && request.User.Identity.IsAuthenticated;
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            return Connection.Broadcast(data);
        }
    }
}
