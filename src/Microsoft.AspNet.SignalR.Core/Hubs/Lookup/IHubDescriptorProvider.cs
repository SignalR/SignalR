// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Hubs
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
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This call might be expensive")]
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
