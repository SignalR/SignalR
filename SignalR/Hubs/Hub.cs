
namespace SignalR.Hubs {
    public class Hub {
        public IClientAgent Agent { get; set; }

        public dynamic Clients {
            get {
                return Agent;
            }
        }

        public dynamic Caller { get; set; }
        public HubContext Context { get; set; }
        public IGroupManager GroupManager { get; set; }

        public void AddToGroup(string groupName) {
            GroupManager.AddToGroup(Context.ClientId, groupName);
        }

        public void RemoveFromGroup(string groupName) {
            GroupManager.RemoveFromGroup(Context.ClientId, groupName);
        }
    }
}