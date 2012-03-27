using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public abstract class Hub : IHub
    {
        public IClientAgent Agent { get; set; }

        public dynamic Clients
        {
            get
            {
                return Agent;
            }
        }

        public dynamic Caller { get; set; }
        public HubContext Context { get; set; }
        public IGroupManager GroupManager { get; set; }

        public Task AddToGroup(string groupName)
        {
            return GroupManager.AddToGroup(Context.ConnectionId, groupName);
        }

        public Task RemoveFromGroup(string groupName)
        {
            return GroupManager.RemoveFromGroup(Context.ConnectionId, groupName);
        }
    }
}