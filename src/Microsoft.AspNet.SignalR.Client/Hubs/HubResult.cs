// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    /// <summary>
    /// Represents the result of a hub invocation.
    /// </summary>
    /// <typeparam name="T">The return type of the hub.</typeparam>
    public class HubResult<T>
    {
        /// <summary>
        /// The return value of the hub
        /// </summary>
        [JsonProperty("R")]
        public T Result { get; set; }
        
        /// <summary>
        /// The error message returned from the hub invocation.
        /// </summary>
        [JsonProperty("E")]
        public string Error { get; set; }

        /// <summary>
        /// The caller state from this hub.
        /// </summary>
        [JsonProperty("S")]
        public IDictionary<string, JToken> State { get; set; }
    }
}
