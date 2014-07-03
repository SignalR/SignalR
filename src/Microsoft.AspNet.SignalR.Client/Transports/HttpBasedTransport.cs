// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : ClientTransportBase
    {
        protected HttpBasedTransport(IHttpClient httpClient, string transportName)
            : base(httpClient, transportName)
        { }

        public override Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var initializeHandler = new TransportInitializationHandler(HttpClient, connection, connectionData, Name, disconnectToken, TransportHelper);

            OnStart(connection, connectionData, disconnectToken, initializeHandler);

            return initializeHandler.Task;
        }

        protected abstract void OnStart(IConnection connection,
                                        string connectionData,
                                        CancellationToken disconnectToken,
                                        TransportInitializationHandler initializeHandler);

        public override Task Send(IConnection connection, string data, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            string url = UrlBuilder.BuildSend(connection, Name, connectionData);

            var postData = new Dictionary<string, string> {
                { "data", data }
            };

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
