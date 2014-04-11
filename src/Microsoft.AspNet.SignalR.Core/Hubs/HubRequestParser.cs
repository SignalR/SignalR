// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubRequestParser : IHubRequestParser
    {
        public HubRequest Parse(string data, JsonSerializer serializer)
        {
            var request = serializer.Parse<HubRequest>(data);
            return request;
        }
    }
}
