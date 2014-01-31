// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public class HubProgressUpdate
    {
        /// <summary>
        /// The callback identifier
        /// </summary>
        [JsonProperty("I")]
        public string Id { get; set; }

        /// <summary>
        /// The progress value
        /// </summary>
        [JsonProperty("D")]
        public JToken Data { get; set; }
    }
}
