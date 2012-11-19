// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public class HubInvocation
    {
        [JsonProperty("I")]
        public string CallbackId { get; set; }

        [JsonProperty("H")]
        public string Hub { get; set; }

        [JsonProperty("M")]
        public string Method { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "This type is used for serialization")]
        [JsonProperty("A")]
        public JToken[] Args { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This type is used for serialization")]
        [JsonProperty("S", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> State { get; set; }
    }
}
