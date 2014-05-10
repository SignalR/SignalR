using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class EchoHub : Hub
    {
        public Task EchoCallback(string message)
        {
            return Clients.Caller.echo(message);
        }

        public async Task EchoAndDelayCallback(string message)
        {
            await Clients.Caller.echo(message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        public string EchoReturn(string message)
        {
            return message;
        }

        public Task SendToUser(string userId, string message)
        {
            return Clients.User(userId).echo(message);
        }
    }
}
