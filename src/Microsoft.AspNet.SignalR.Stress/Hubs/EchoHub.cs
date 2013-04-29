﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Stress.Hubs
{
    public class EchoHub : Hub
    {
        public Task Echo(string message)
        {
            return Clients.Caller.echo(message);
        }  
    }
}
