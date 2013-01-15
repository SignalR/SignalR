// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubRequestParser : IHubRequestParser
    {
        private static readonly IJsonValue[] _emptyArgs = new IJsonValue[0];

        public HubRequest Parse(string data)
        {
            var rawRequest = JObject.Parse(data);
            var request = new HubRequest();

            request.Hub = rawRequest.Value<string>("H");
            request.Method = rawRequest.Value<string>("M");
            request.Id = rawRequest.Value<string>("I");

            var rawState = rawRequest["S"];
            request.State = rawState == null ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) :
                                       rawState.ToObject<IDictionary<string, object>>();

            var rawArgs = rawRequest["A"];
            request.ParameterValues = rawArgs == null ? _emptyArgs :
                                                rawArgs.Children().Select(value => new JTokenValue(value)).ToArray();

            return request;
        }

    }
}
