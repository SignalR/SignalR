namespace SignalR.Hubs
{
    public interface IHub
    {
        /// <summary>
        /// Gets a dynamic object that represents the calling client.
        /// </summary>
        dynamic Caller { get; set; }

        /// <summary>
        /// Gets a <see cref="HubCallerContext"/>. Which contains information about the calling client.
        /// </summary>
        HubCallerContext Context { get; set; }

        /// <summary>
        /// Gets a dynamic object that represents all clients connected to this hub (not hub instance).
        /// </summary>
        dynamic Clients { get; set; }

        /// <summary>
        /// Gets the <see cref="IGroupManager"/> the hub instance.
        /// </summary>
        IGroupManager Groups { get; set; }
    }
}

