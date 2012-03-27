namespace SignalR.Hubs.Lookup
{
    using System;
    using System.Collections.Generic;

    using SignalR.Hubs.Lookup.Descriptors;

    /// <summary>
    /// Describes a hub manager - main point in the whole hub and action lookup process.
    /// </summary>
    public interface IHubManager
    {
        /// <summary>
        /// Retrieves a single hub descriptor.
        /// </summary>
        /// <param name="hubName">Name of the hub.</param>
        /// <returns>Hub descriptor, if found. Null, otherwise.</returns>
        HubDescriptor GetHub(string hubName);

        /// <summary>
        /// Retrieves all available hubs.
        /// </summary>
        /// <returns>List of hub descriptors.</returns>
        IEnumerable<HubDescriptor> GetHubs(Predicate<HubDescriptor> predicate = null);

        /// <summary>
        /// Resolves a given hub name to a concrete object.
        /// </summary>
        /// <param name="hubName">Name of the hub.</param>
        /// <returns>Hub implementation instance, if found. Null otherwise.</returns>
        IHub ResolveHub(string hubName);

        /// <summary>
        /// Resolves all available hubs to their concrete objects.
        /// </summary>
        /// <returns>List of hub instances.</returns>
        IEnumerable<IHub> ResolveHubs();

        /// <summary>
        /// Retrieves an action of a given name on a given hub.
        /// </summary>
        /// <param name="hubName">Name of the hub.</param>
        /// <param name="actionName">Name of the action.</param>
        /// <returns>Descriptor of the action, if found. Null otherwise.</returns>
        ActionDescriptor GetHubAction(string hubName, string actionName, params object[] parameters);

        /// <summary>
        /// Gets all actions available to call on a given hub.
        /// </summary>
        /// <param name="hubName">Name of the hub,</param>
        /// <returns>List of available actions.</returns>
        IEnumerable<ActionDescriptor> GetHubActions(string hubName, Predicate<ActionDescriptor> predicate = null);
    }
}