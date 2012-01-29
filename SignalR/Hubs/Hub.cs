using System.Threading.Tasks;
using SignalR.Infrastructure;
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
 
        public static dynamic GetClients<T>(IDependencyResolver resolver) where T : IHub
        {
            return GetClients(typeof(T).FullName, resolver);
        }

        public static dynamic GetClients(string hubName, IDependencyResolver resolver)
        {
            var connection = Connection.GetConnection<HubDispatcher>(resolver);
            return new ClientAgent(connection, hubName);
        }
    }
}