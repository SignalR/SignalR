namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public class HubConnectionContext
    {
        private readonly string _hubName;

        /// <summary>
        /// 
        /// </summary>
        public HubConnectionContext()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="hubName"></param>
        /// <param name="connectionId"></param>
        /// <param name="state"></param>
        public HubConnectionContext(IConnection connection, string hubName, string connectionId, TrackingDictionary state)
        {
            Caller = new StatefulSignalProxy(connection, connectionId, hubName, state);
            All = new ClientProxy(connection, hubName);
            Others = new ClientProxy(connection, hubName, sendToAll: false);

            Connection = connection;
            _hubName = hubName;
        }

        /// <summary>
        /// The connection to all hubs.
        /// </summary>
        public IConnection Connection { get; private set; }

        /// <summary>
        /// All connected clients.
        /// </summary>
        public dynamic All { get; set; }

        /// <summary>
        /// All connected clients except the calling client.
        /// </summary>
        public dynamic Others { get; set; }

        /// <summary>
        /// Represents the calling client.
        /// </summary>
        public dynamic Caller { get; set; }

        /// <summary>
        /// Get a dynamic representation of the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <returns></returns>
        public dynamic Group(string groupName)
        {
            return new SignalProxy(Connection, groupName, _hubName);
        }

        /// <summary>
        /// Returns a dynamic representation of the connection with the specified connectionid.
        /// </summary>
        /// <param name="connectionId">The connection id</param>
        /// <returns></returns>
        public dynamic Client(string connectionId)
        {
            return new SignalProxy(Connection, connectionId, _hubName);
        }
    }
}
