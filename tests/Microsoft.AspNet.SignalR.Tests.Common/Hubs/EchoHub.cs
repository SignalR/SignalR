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
