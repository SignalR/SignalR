using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace AzureTestManager
{
    public class TestManagerHub : Hub
    {
        public void Join(string group)
        {
            Groups.Add(Context.ConnectionId, group);
        }

        public void JoinConnectionGroup()
        {
            Groups.Add(Context.ConnectionId, Context.ConnectionId);
        }

        public void StartProcesses(string connectionId, int instances, string argumentString)
        {
            Clients.Group(connectionId).startProcesses(instances, argumentString);
        }

        public void StopProcess(string connectionId, string processId)
        {
            Clients.Group(connectionId).stopProcess(processId);
        }

        public void AddUpdateWorker(string address, string status)
        {
            Clients.Group("manager").addUpdateWorker(
                Context.ConnectionId,
                address,
                status);
        }

        public void AddUpdateProcess(string id, string status)
        {
            Clients.Group("manager").addUpdateProcess(
                Context.ConnectionId,
                id,
                status);
        }

        public void RemoveProcess(string id)
        {
            Clients.Group("manager").removeProcess(
                Context.ConnectionId,
                id);
        }

        public void AddTrace(string address, string message)
        {
            Clients.Group("manager").addTrace(address, message);
        }

        public override Task OnDisconnected()
        {
            Clients.Group("manager").disconnected(Context.ConnectionId);
            return base.OnDisconnected();
        }
    }
}
