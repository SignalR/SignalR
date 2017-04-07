// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class FallbackToLongPollingConnectionThrows : PersistentConnection
    {
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            throw new InvalidOperationException();
        }
    }
}
