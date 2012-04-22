using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public abstract class Hub : IHub
    {
        public IClientAgent Agent { get; set; }

        /// <summary>
        /// A dynamic object that represents all clients connected to this hub (not hub instance).
        /// </summary>
        public dynamic Clients
        {
            get
            {
                return Agent;
            }
        }

        /// <summary>
        /// A dynamic object that represents the calling client.
        /// </summary>
        public dynamic Caller { get; set; }

        /// <summary>
        /// Provides information about the calling client.
        /// </summary>
        public HubContext Context { get; set; }

        /// <summary>
        /// The group manager for this hub instance.
        /// </summary>
        public IGroupManager GroupManager { get; set; }

        /// <summary>
        /// Adds the calling client to the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A task that represents the calling client being added to the group.</returns>
        public Task AddToGroup(string groupName)
        {
            return GroupManager.AddToGroup(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Remove the calling client to the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <returns>A task that represents the calling client being removed from the group.</returns>
        public Task RemoveFromGroup(string groupName)
        {
            return GroupManager.RemoveFromGroup(Context.ConnectionId, groupName);
        }
    }
}