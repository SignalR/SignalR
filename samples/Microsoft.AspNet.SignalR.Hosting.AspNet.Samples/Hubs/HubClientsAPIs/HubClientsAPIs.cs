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

        public void DisplayMessageAllExcept(string connectionId, string message, params string[] targetConnectionId)
        {
            Clients.AllExcept(targetConnectionId).displayMessage("From Clients.AllExcept: " + message + " " + connectionId);
        }

        public void DisplayMessageOther(string connectionId, string message)
        {
            Clients.Others.displayMessage("From Clients.Others: " + message + " " + connectionId);
        }

        public void DisplayMessageCaller(string connectionId, string message)
        {
            Clients.Caller.displayMessage("From Clients.Caller: " + message + " " + connectionId);
        }

        public void DisplayMessageSpecified(string connectionId, string targetConnectionId, string message)
        {
            Clients.Client(targetConnectionId).displayMessage("From Clients.Client: " + message + " " + connectionId);
        }

        public string JoinGroup(string connectionId, string groupName)
        {
            Groups.Add(connectionId, groupName);
            return connectionId + " joined " + groupName;
        }

        public string LeaveGroup(string connectionId, string groupName)
        {
            Groups.Remove(connectionId, groupName);
            return connectionId + " removed " + groupName;
        }

        public void DisplayMessageGroup(string connectionId, string groupName, string message)
        {
            Clients.Group(groupName, "").displayMessage("From Clients.Group: " + message + " " + connectionId);
        }
        
        public void DisplayMessageOthersInGroup(string connectionId, string groupName, string message)
        {
            Clients.OthersInGroup(groupName).displayMessage("From Clients.OthersInGroup: " + message + " " + connectionId);
        }
        
        public override Task OnConnected()
        {
            return Clients.All.displayMessage(Context.ConnectionId + " OnConnected");
        }

        public override Task OnReconnected()
        {
            return Clients.Caller.displayMessage(Context.ConnectionId + " OnReconnected");
        }

        public override Task OnDisconnected()
        {
            return Clients.All.displayMessage(Context.ConnectionId + " OnDisconnected");
        }
    }
}