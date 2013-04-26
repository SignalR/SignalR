using System.Threading;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub.MessageLoops
{
    public class MessageLoops : Hub
    {
        public string JoinGroup(string connectionId, string groupName)
        {
            Groups.Add(connectionId, groupName).Wait();
            return connectionId + " joined " + groupName;
        }

        public string LeaveGroup(string connectionId, string groupName)
        {
            Groups.Remove(connectionId, groupName).Wait();
            return connectionId + " removed " + groupName;
        }

        public int SendMessageCountToAll(int message)
        {
            Thread.Sleep(5000);
            Clients.All.displayMessagesCount(++message, Context.ConnectionId);
            return message;
        }
        
        public int SendMessageCountToGroup(int message, string groupName)
        {
            Thread.Sleep(5000);
            Clients.Group(groupName).displayMessagesCount(++message, Context.ConnectionId);
            return message;
        }


        public int SendMessageCountToCaller(int message)
        {
            Thread.Sleep(5000);
            Clients.Caller.displayMessagesCount(++message, Context.ConnectionId);
            return message;
        }
    }

}