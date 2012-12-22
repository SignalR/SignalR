// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Security.Principal;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// Used to generate prefixes for connection ids.
    /// </summary>
    public interface IConnectionIdPrefixGenerator
    {
        /// <summary>
        /// Creates a prefix for a connection id generated in response to a negotiation request.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/>.</param>
        /// <returns>A connection id prefix</returns>
        string GenerateConnectionIdPrefix(IRequest request);
    }
}
