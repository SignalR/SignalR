using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.HubClientsAPIs
{
    public class HubClientsAPIs : Hub
    {
        public void DisplayMessageAll(string connectionId, string message)
        {
            Clients.All.displayMessage("From Clients.All: " + message + " " + connectionId);
        }

        public void DisplayMessageAllExcept(string connectionId, string message, params string[] target_connectionId)
        {
            Clients.AllExcept(target_connectionId).displayMessage("From Clients.AllExcept: " + message + " " + connectionId);
        }

        public void DisplayMessageOther(string connectionId, string message)
        {
            Clients.Others.displayMessage("From Clients.Others: " + message + " " + connectionId);
        }

        public void DisplayMessageCaller(string connectionId, string message)
        {
            Clients.Caller.displayMessage("From Clients.Caller: " + message + " " + connectionId);
        }

        public void DisplayMessageSpecified(string connectionId, string target_connectionId, string message)
        {
            Clients.Client(target_connectionId).displayMessage("From Clients.Client: " + message + " " + connectionId);
        }

        public string JoinGroup(string connectionId, string groupNme)
        {
            Groups.Add(connectionId, groupNme);
            return connectionId + " joined " + groupNme;
        }

        public string LeaveGroup(string connectionId, string groupNme)
        {
            Groups.Remove(connectionId, groupNme);
            return connectionId + " removed " + groupNme;
        }

        public void DisplayMessageGroup(string connectionId, string groupNme, string message)
        {
            Clients.Group(groupNme, "").displayMessage("From Clients.Group: " + message + " " + connectionId);
        }
        
        public void DisplayMessageOthersInGroup(string connectionId, string groupNme, string message)
        {
            Clients.OthersInGroup(groupNme).displayMessage("From Clients.OthersInGroup: " + message + " " + connectionId);
        }
        
        public override Task OnConnected()
        {
            Clients.All.displayMessage(Context.ConnectionId + " OnConnected");
            return null;
        }

        public override Task OnReconnected()
        {
            Clients.Caller.displayMessage(Context.ConnectionId + " OnReconnected");
            return null;
        }

        public override Task OnDisconnected()
        {
            Clients.All.displayMessage(Context.ConnectionId + " OnDisconnected");
            return null;
        }
    }
}