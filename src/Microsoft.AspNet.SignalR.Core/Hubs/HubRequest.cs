// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class HubRequest
    {
        [JsonProperty("H")]
        public string Hub { get; set; }

        [JsonProperty("M")]
        public string Method { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "This type is used for de-serialization.")]
        [JsonProperty("A")]
        public IJsonValue[] ParameterValues { get; set; }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This type is used for de-serialization.")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "This type is used for de-serialization.")]
        [JsonProperty("S")]
        public JObject State { get; set; }

        [JsonProperty("I")]
        public string Id { get; set; }
    }
}
