namespace SignalR.Hubs
{
    internal class HubContext : IHubContext
    {
        public HubContext(dynamic clients, IGroupManager groupManager)
        {
            Clients = clients;
            Groups = groupManager;
        }

        public dynamic Clients { get; private set; }

        public IGroupManager Groups { get; private set; }
    }
}
