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


namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.HubClientsAPIs
{    
    public class HubClientsAPIs : Hub
    {             
        public string getMessageAll(string connectionId, string message)
        {
            Clients.All.foo("From Clients.All: " + message + " " + connectionId);
            return message;
        }
  
        public void getMessageAllExcept(string connectionId,  string message, params string[] target_connectionId)
        {
            Clients.AllExcept(target_connectionId).foo("From Clients.AllExcept: " + message + " " + connectionId);
        }

        public void getMessageOther(string connectionId, string message)
        {
            Clients.Others.foo("From Clients.Others: " + message + " " + connectionId);
        }

        public string getMessageCaller(string connectionId, string message)
        {
            Clients.Caller.foo("From Clients.Caller: " + message + " " + connectionId );
            return message;
        }

        public void getMessageSpecified(string connectionId, string target_connectionId, string message)
        {
            Clients.Client(target_connectionId).foo("From Clients.Client: " + message + " " + connectionId); 
        }

        public string joinGroup(string connectionId, string groupNme)
        {
            Groups.Add(connectionId, groupNme);
            return connectionId + " joined " + groupNme;;
        }

        public string leaveGroup(string connectionId, string groupNme)
        {
            Groups.Remove(connectionId, groupNme);
            return connectionId + " removed " + groupNme;
        }

        public void getMessageGroup(string connectionId, string groupNme, string message)
        {
            Clients.Group(groupNme, "").foo("From Clients.Group: " + message + " " + connectionId);
        }


        public void getMessageOthersInGroup(string connectionId, string groupNme, string message)
        {
            Clients.OthersInGroup(groupNme).foo("From Clients.OthersInGroup: " + message + " " + connectionId);
        }


        public override Task OnConnected()
        {
            Clients.All.foo(Context.ConnectionId + " OnConnected");      
            return null;
        }

        public override Task OnReconnected()
        {
            Clients.Caller.foo(Context.ConnectionId + " OnReconnected" );   
            return null;
        }

        public override Task OnDisconnected()
        {
            Clients.All.foo( Context.ConnectionId + " OnDisconnected");
            return null;            
        }

    }
        
}