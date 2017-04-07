// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    [Authorize(Users="User")]
    [Authorize(Roles="Admin")]
    public class UserAndRoleAuthHub : NoAuthHub
    {
    }
}