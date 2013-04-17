using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub.MessageLoops
{
    public class MessageLoops : Hub
    {
        public int SendMessageCount(int message, string connectionId)
        {
            Thread.Sleep(5000);
            Clients.All.displayMessagesCount(++message, connectionId);
            return message;
        }
    }

}