namespace SignalR
{
    internal class PersistentConnectionContext : IPersistentConnectionContext
    {
        public PersistentConnectionContext(IConnection connection, IConnectionGroupManager groupManager)
        {
            Connection = connection;
            Groups = groupManager;
        }

        public IConnection Connection { get; private set; }

        public IConnectionGroupManager Groups { get; private set; }
    }
}
