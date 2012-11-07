// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public class HubInvocation
    {
        [JsonProperty("H")]
        public string Hub { get; set; }

        [JsonProperty("M")]
        public string Method { get; set; }

        [JsonProperty("A")]
        public JToken[] Args { get; set; }

        [JsonProperty("S", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, JToken> State { get; set; }
    }

    public class ServerHubInvocation
    {
        public string Hub { get; set; }
        public string Method { get; set; }
        public JToken[] Args { get; set; }
        public Dictionary<string, JToken> State { get; set; }
    }
}
