// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Provides access to information about a <see cref="IHub"/>.
    /// </summary>
    public interface IHubContext
    {
        /// <summary>
        /// The serializer associated with the <see cref="IHubContext"/>.
        /// </summary>
        JsonSerializer Serializer { get; } 

        /// <summary>
        /// Gets the <see cref="IDuplexConnection" /> for the <see cref="IHub" />.
        /// </summary>
        IDuplexConnection Connection { get; }

        /// <summary>
        /// Encapsulates all information about a SignalR connection for an <see cref="IHub"/>.
        /// </summary>
        IHubConnectionContext<dynamic> Clients { get; }

        /// <summary>
        /// Gets the <see cref="IGroupManager"/> the hub.
        /// </summary>
        IGroupManager Groups { get; }
    }
}
