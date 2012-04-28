using System.Collections.Generic;

namespace SignalR.Hubs
{
    /// <summary>
    /// Describes hub descriptor provider, which provides information about available hubs.
    /// </summary>
    public interface IHubDescriptorProvider
    {
        /// <summary>
        /// Retrieve all avaiable hubs.
        /// </summary>
        /// <returns>Collection of hub descriptors.</returns>
        IList<HubDescriptor> GetHubs();

        /// <summary>
        /// Tries to retrieve hub with a given name.
        /// </summary>
        /// <param name="hubName">Name of the hub.</param>
        /// <param name="descriptor">Retrieved descriptor object.</param>
        /// <returns>True, if hub has been found</returns>
        bool TryGetHub(string hubName, out HubDescriptor descriptor);
    }
}