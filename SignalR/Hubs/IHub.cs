namespace SignalR.Hubs
{
    public interface IHub
    {
        /// <summary>
        /// A dynamic object that represents all clients connected to this hub (not hub instance).
        /// </summary>
        dynamic Clients { get; set; }

        /// <summary>
        /// A dynamic object that represents the calling client.
        /// </summary>
        dynamic Caller { get; set; }

        /// <summary>
        /// Provides information about the calling client.
        /// </summary>
        HubContext Context { get; set; }

        /// <summary>
        /// The group manager for this hub instance.
        /// </summary>
        IGroupManager Groups { get; set; }
    }
}

