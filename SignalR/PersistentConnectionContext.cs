namespace SignalR
{
    internal class PersistentConnectionContext : IPersistentConnectionContext
    {
        public PersistentConnectionContext(IConnection connection, IGroupManager groupManager)
        {
            Connection = connection;
            Groups = groupManager;
        }

        public IConnection Connection { get; private set; }

        public IGroupManager Groups { get; private set; }
    }
}
