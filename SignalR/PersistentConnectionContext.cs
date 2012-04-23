namespace SignalR
{
    /// <summary>
    /// Contains information about a <see cref="PersistentConnection"/>.
    /// </summary>
    public class PersistentConnectionContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PersistentConnectionContext"/>.
        /// </summary>
        /// <param name="connection">The connection for the <see cref="PersistentConnectionContext"/>.</param>
        /// <param name="groupManager">The group manager for the <see cref="PersistentConnectionContext"/>.</param>
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
