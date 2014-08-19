using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Stress.Infrastructure
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
