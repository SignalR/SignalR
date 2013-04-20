using System.Threading;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub.MessageLoops
{
    public class MessageLoops : Hub
    {
        public int SendMessageCount(int message)
        {
            Thread.Sleep(5000);
            Clients.All.displayMessagesCount(++message, Context.ConnectionId);
            return message;
        }
    }

}