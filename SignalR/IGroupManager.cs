namespace SignalR {
    public interface IGroupManager {
        void AddToGroup(string clientId, string groupName);
        void RemoveFromGroup(string clientId, string groupName);
    }
}
