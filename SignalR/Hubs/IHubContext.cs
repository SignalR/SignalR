using System;

namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubContext
    {
        /// <summary>
        /// Gets a dynamic object that represents all clients connected to the hub.
        /// </summary>
        dynamic Clients { get; }

        /// <summary>
        /// Gets the <see cref="IGroupManager"/> the hub.
        /// </summary>
        IGroupManager Groups { get; }
    }
}
