using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Samples.Hubs.HubConnectionAPI
{
    public class HubConnectionAPI : Hub
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

        public void DisplayMessageAll(string message)
        {
            Clients.All.displayMessage("Clients.All from " + Context.ConnectionId + ": " + message);
        }

        public void DisplayMessageAllExcept(string message, params string[] excludeConnectionIds)
        {
            Clients.AllExcept(excludeConnectionIds).displayMessage("Clients.AllExcept: " + message + " from " + Context.ConnectionId);
        }

        public void DisplayMessageOther(string message)
        {
            Clients.Others.displayMessage("Clients.Others: " + message + " from " + Context.ConnectionId);
        }

        public void DisplayMessageCaller(string message)
        {
            Clients.Caller.displayMessage("Clients.Caller: " + message + " from " + Context.ConnectionId);
        }

        public void DisplayMessageSpecified(string targetConnectionId, string message)
        {
            Clients.Client(targetConnectionId).displayMessage("Clients.Client: " + message + " from " + Context.ConnectionId);
        }

        public void DisplayMessageGroup(string groupName, string message)
        {
            Clients.Group(groupName).displayMessage("Clients.Group: " + message + " from " + Context.ConnectionId);
        }

        public void DisplayMessageGroupExcept(string groupName, string message, params string[] excludeConnectionIds)
        {
            Clients.Group(groupName, excludeConnectionIds).displayMessage("Clients.Group: " + message + " from " + Context.ConnectionId);
        }

        public void DisplayMessageOthersInGroup(string groupName, string message)
        {
            Clients.OthersInGroup(groupName).displayMessage("Clients.OthersInGroup: " + message + " from" + Context.ConnectionId);
        }

        public override Task OnConnected()
        {
            return Clients.All.displayMessage(Context.ConnectionId + " OnConnected");
        }

        public override Task OnReconnected()
        {
            return Clients.Caller.displayMessage(Context.ConnectionId + " OnReconnected");
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var message = string.Format("{0} OnDisconnected(stopCalled: {1})", Context.ConnectionId, stopCalled);
            return Clients.All.displayMessage(message);
        }
    }
}