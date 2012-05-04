namespace SignalR.Hubs
{
    /// <summary>
    /// Provides methods that communicate with SignalR connections that connected to a <see cref="Hub"/>.
    /// </summary>
    public abstract class Hub : IHub
    {
        /// <summary>
        /// A dynamic object that represents all clients connected to this hub (not hub instance).
        /// </summary>
        public dynamic Clients { get; set; }

        /// <summary>
        /// A dynamic object that represents the calling client.
        /// </summary>
        public dynamic Caller { get; set; }

        /// <summary>
        /// Provides information about the calling client.
        /// </summary>
        public HubCallerContext Context { get; set; }

        /// <summary>
        /// The group manager for this hub instance.
        /// </summary>
        public IGroupManager Groups { get; set; }
    }
}