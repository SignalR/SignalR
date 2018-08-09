// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Core.Hubs
{
    // These classes are used by the Core/Hubs XUnit tests.

    public class NotAHub
    {
    }

    public class CoreTestHub : Hub
    {
    }

    [HubName("CoreHubWithAttribute")]
    public class CoreTestHubWithAttribute : Hub
    {
    }

    public class CoreTestHubWithMethod : Hub
    {
        public int AddNumbers(int first, int second)
        {
            return first + second;
        }
    }
}
