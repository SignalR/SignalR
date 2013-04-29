using System;
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
            return connectionId + " removed from " + groupName;
        }

        public int SendMessageCountToAll(int messageCount, string sleep)
        {
            if (Convert.ToDouble(sleep) > 0)
                Thread.Sleep(Convert.ToInt32(Convert.ToDouble(sleep) * 1000));

            Clients.All.displayMessagesCount(++messageCount, Context.ConnectionId).Wait();
            return messageCount;
        }

        public int SendMessageCountToGroup(int messageCount, string groupName, string sleep)
        {
            if (Convert.ToDouble(sleep) > 0)
                Thread.Sleep(Convert.ToInt32(Convert.ToDouble(sleep) * 1000));

            Clients.Group(groupName).displayMessagesCount(++messageCount, Context.ConnectionId).Wait();
            return messageCount;
        }


        public int SendMessageCountToCaller(int messageCount, string sleep)
        {
            if (Convert.ToDouble(sleep) > 0)
                Thread.Sleep(Convert.ToInt32(Convert.ToDouble(sleep) * 1000));

            Clients.Caller.displayMessagesCount(++messageCount, Context.ConnectionId).Wait();
            return messageCount;
        }
    }

}