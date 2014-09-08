// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : ClientTransportBase
    {
        protected HttpBasedTransport(IHttpClient httpClient, string transportName)
            : base(httpClient, transportName)
        { }

        public override Task Send(IConnection connection, string data, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            string url = UrlBuilder.BuildSend(connection, Name, connectionData);

            var postData = new Dictionary<string, string> { { "data", data } };

            return HttpClient.Post(url, connection.PrepareRequest, postData, isLongRunning: false)
                .Then(response => response.ReadAsString())
                .Then(raw =>
                {
                    if (!String.IsNullOrEmpty(raw))
                    {
                        connection.Trace(TraceLevels.Messages, "OnMessage({0})", raw);

                        connection.OnReceived(connection.JsonDeserializeObject<JObject>(raw));
                    }
                })
                .Catch(connection.OnError, connection);
        }
    }
}
