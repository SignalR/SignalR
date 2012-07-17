using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SignalR.Hubs
{
    internal class HubRequestParser : IHubRequestParser
    {
        private static readonly IJsonValue[] _emptyArgs = new IJsonValue[0];

        public HubRequest Parse(string data)
        {
            var rawRequest = JObject.Parse(data);
            var request = new HubRequest();

            // TODO: Figure out case insensitivity in JObject.Parse, this should cover our clients for now
            request.Hub = rawRequest.Value<string>("hub") ?? rawRequest.Value<string>("Hub");
            request.Method = rawRequest.Value<string>("method") ?? rawRequest.Value<string>("Method");
            request.Id = rawRequest.Value<string>("id") ?? rawRequest.Value<string>("Id");

            var rawState = rawRequest["state"] ?? rawRequest["State"];
            request.State = rawState == null ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) :
                                       rawState.ToObject<IDictionary<string, object>>();

            var rawArgs = rawRequest["args"] ?? rawRequest["Args"];
            request.ParameterValues = rawArgs == null ? _emptyArgs :
                                                rawArgs.Children().Select(value => new JTokenValue(value)).ToArray();

            return request;
        }

    }
}
