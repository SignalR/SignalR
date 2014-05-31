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

        public void AddUpdateWorker(string guid, string address, string status)
        {
            Clients.Group("manager").addUpdateWorker(
                guid,
                address,
                status);
        }

        public void AddUpdateProcess(string guid, string id, string status)
        {
            Clients.Group("manager").addUpdateProcess(
                guid,
                id,
                status);
        }

        public void AddErrorTrace(string guid, string id, string message)
        {
            Clients.Group("manager").addErrorTrace(guid, id, message);
        }

        public void AddOutputTrace(string guid, string id, string message)
        {
            Clients.Group("manager").addOutputTrace(guid, id, message);
        }

        public override Task OnDisconnected()
        {
            Clients.Group("manager").disconnected(Context.ConnectionId);
            return base.OnDisconnected();
        }
    }
}
