namespace SignalR
{
    public class PersistentConnectionContext
    {
        public PersistentConnectionContext(IConnection connection, IGroupManager groupManager)
        {
            Connection = connection;
            GroupManager = groupManager;
        }

        /// <summary>
        /// Gets the <see cref="IConnection"/> for the <see cref="PersistentConnection"/>
        /// </summary>
        public IConnection Connection { get; private set; }

        /// <summary>
        /// Gets the <see cref="IGroupManager"/> for the <see cref="PersistentConnection"/>
        /// </summary>
        public IGroupManager GroupManager { get; private set; }
    }
}
