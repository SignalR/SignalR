// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Newtonsoft.Json;
namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Handles parsing incoming requests through the <see cref="HubDispatcher"/>.
    /// </summary>
    public interface IHubRequestParser
    {
        /// <summary>
        /// Parses the incoming hub payload into a <see cref="HubRequest"/>.
        /// </summary>
        /// <param name="data">The raw hub payload.</param>
        /// <param name="serializer">The JsonSerializer used to parse the data.</param>
        /// <returns>The resulting <see cref="HubRequest"/>.</returns>
        HubRequest Parse(string data, JsonSerializer serializer);
    }
}
