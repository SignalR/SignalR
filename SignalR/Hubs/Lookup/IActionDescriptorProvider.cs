namespace SignalR.Hubs.Lookup
{
    using System.Collections.Generic;

    using SignalR.Hubs.Lookup.Descriptors;

    /// <summary>
    /// Describes a hub action provider that builds a collection of available actions on a given hub.
    /// </summary>
    public interface IActionDescriptorProvider
    {
        /// <summary>
        /// Retrieve all actions on a given hub.
        /// </summary>
        /// <param name="hub">Hub descriptor object.</param>
        /// <returns>Available actions.</returns>
        IEnumerable<ActionDescriptor> GetActions(HubDescriptor hub);

        /// <summary>
        /// Tries to retrieve an action using this provider.
        /// </summary>
        /// <param name="hub">Hub descriptor object</param>
        /// <param name="action">Name of the action.</param>
        /// <param name="descriptor">Descriptor of an action, if found. Null otherwise.</param>
        /// <returns>If action has been found.</returns>
        bool TryGetAction(HubDescriptor hub, string action, out ActionDescriptor descriptor, params object[] parameters);
    }
}