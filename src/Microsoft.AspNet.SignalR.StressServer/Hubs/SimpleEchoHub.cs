// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.StressServer.Hubs
{
    public class SimpleEchoHub : Hub
    {   
        public Task Echo(string message)
        {
            return Clients.Caller.echo(message);
        }

        public Task Send(int number)
        {
            return Clients.All.send(number, Context.ConnectionId, Context.Headers["X-Server-Name"]);
        }
    }
}
