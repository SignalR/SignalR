using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Web.Configuration;
using System.Configuration;


namespace Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub.MessagegsLoops
{
    public class MessagegsLoops : Hub
{  
    public int SendMessageCount( int message, string connectionId)
    {
        Thread.Sleep(5000);
        Clients.All.displayMessagesCount(++message, connectionId);
        return message;
    }
}

}