using System;

namespace SignalR.Hubs
{
    /// <summary>
    /// Provides access to information about a <see cref="IHub"/>.
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

        /// <summary>
        /// Returns a dynamic representation of the specified group.
        /// </summary>
        /// <param name="groupName">The name of the group</param>
        /// <param name="exclude">A list of connection ids to exclude.</param>
        /// <returns>A dynamic representation of the specified group.</returns>
        dynamic Group(string groupName, params string[] exclude);

        /// <summary>
        /// Returns a dynamic representation of the connection with the specified connectionid.
        /// </summary>
        /// <param name="connectionId">The connection id</param>
        /// <returns>A dynamic representation of the connection with the specified connectionid.</returns>
        dynamic Client(string connectionId);
    }
}
