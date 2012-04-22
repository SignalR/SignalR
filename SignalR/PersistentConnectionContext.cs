namespace SignalR
{
    public class PersistentConnectionContext
    {
        public PersistentConnectionContext(IConnection connection, IGroupManager groupManager)
        {
            Connection = connection;
            GroupManager = groupManager;
        }

        public IConnection Connection { get; private set; }
        public IGroupManager GroupManager { get; private set; }
    }
}
