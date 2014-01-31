// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    /// <summary>
    /// Represents the result of a hub invocation.
    /// </summary>
    public class HubResult
    {
        /// <summary>
        /// The callback identifier
        /// </summary>
        [JsonProperty("I")]
        public string Id { get; set; }

        /// <summary>
        /// The progress update of the invocation
        /// </summary>
        [JsonProperty("P")]
        public HubProgressUpdate ProgressUpdate { get; set; }

        /// <summary>
        /// The return value of the hub
        /// </summary>
        [JsonProperty("R")]
        public JToken Result { get; set; }

        /// <summary>
        /// Indicates whether the Error is a <see cref="HubException"/>.
        /// </summary>
        [JsonProperty("H")]
        public bool? IsHubException { get; set; }

        /// <summary>
        /// The error message returned from the hub invocation.
        /// </summary>
        [JsonProperty("E")]
        public string Error { get; set; }

        /// <summary>
        /// Extra error data
        /// </summary>
        [JsonProperty("D")]
        public object ErrorData { get; set; }

        /// <summary>
        /// The caller state from this hub.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Type is used for serialization.")]
        [JsonProperty("S")]
        public IDictionary<string, JToken> State { get; set; }
    }
}
