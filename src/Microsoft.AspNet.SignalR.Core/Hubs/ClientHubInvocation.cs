// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientHubInvocation
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public string Target { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("H")]
        public string Hub { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("M")]
        public string Method { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Type is used for serialization.")]
        [JsonProperty("A")]
        public object[] Args { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Type is used for serialization.")]
        [JsonProperty("S", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> State { get; set; }
    }
}
