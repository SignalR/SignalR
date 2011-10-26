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
            return GroupManager.AddToGroup(Context.ClientId, groupName);
        }

        public Task RemoveFromGroup(string groupName)
        {
            return GroupManager.RemoveFromGroup(Context.ClientId, groupName);
        }

        public static Task Invoke<T>(string method, params object[] args) where T : IHub
        {
            return Invoke(typeof(T).FullName, method, args);
        }

        public static Task Invoke(string hubName, string method, params object[] args)
        {
            var connection = Connection.GetConnection<HubDispatcher>();
            return ClientAgent.Invoke(connection, method, hubName, method, args);
        }

        public static dynamic GetClients<T>() where T : IHub
        {
            return GetClients(typeof(T).FullName);
        }

        public static dynamic GetClients(string hubName)
        {
            var connection = Connection.GetConnection<HubDispatcher>();
            return new ClientAgent(connection, hubName);
        }
    }
}