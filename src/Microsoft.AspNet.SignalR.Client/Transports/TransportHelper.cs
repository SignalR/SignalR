// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.SignalR.Client.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class TransportHelper
    {
        // virtual to allow mocking
        public virtual Task<NegotiationResponse> GetNegotiationResponse(IHttpClient httpClient, IConnection connection, string connectionData)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var negotiateUrl = new UrlBuilder().BuildNegotiate(connection, connectionData);

            httpClient.Initialize(connection);

            return httpClient.Get(negotiateUrl, connection.PrepareRequest, isLongRunning: false)
                            .Then(response => response.ReadAsString())
                            .Then(raw =>
                            {
                                if (String.IsNullOrEmpty(raw))
                                {
                                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ServerNegotiationFailed));
                                }

                                return JsonConvert.DeserializeObject<NegotiationResponse>(raw);
                            });
        }

        // virtual to allow mocking
        public virtual Task<string> GetStartResponse(IHttpClient httpClient, IConnection connection, string connectionData, string transport)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var startUrl = new UrlBuilder().BuildStart(connection, transport, connectionData);

            httpClient.Initialize(connection);

            return httpClient.Get(startUrl, connection.PrepareRequest, isLongRunning: false)
                            .Then(response => response.ReadAsString());
        }

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "This is called internally.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The client receives the exception in the OnError callback.")]
        // virtual to allow mocking
        public virtual void ProcessResponse(IConnection connection, string response, out bool shouldReconnect, out bool disconnected, Action onInitialized)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            connection.MarkLastMessage();

            shouldReconnect = false;
            disconnected = false;

            if (String.IsNullOrEmpty(response))
            {
                return;
            }

            try
            {
                var result = connection.JsonDeserializeObject<JObject>(response);

                if (!result.HasValues)
                {
                    return;
                }

                if (result["I"] != null)
                {
                    connection.OnReceived(result);
                    return;
                }

                shouldReconnect = (int?)result["T"] == 1;
                disconnected = (int?)result["D"] == 1;

                if (disconnected)
                {
                    return;
                }

                UpdateGroups(connection, groupsToken: result["G"]);

                var messages = result["M"] as JArray;
                if (messages != null)
                {
                    connection.MessageId = (string)result["C"];

                    foreach (JToken message in (IEnumerable<JToken>)messages)
                    {
                        connection.OnReceived(message);
                    }

                    TryInitialize(result, onInitialized);
                }
            }
            catch (Exception ex)
            {
                connection.OnError(ex);
            }
        }

        public static bool VerifyLastActive(IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            // Ensure that we have not exceeded the reconnect window
            if (DateTime.UtcNow - connection.LastActiveAt >= connection.ReconnectWindow)
            {
                connection.Trace(TraceLevels.Events, "There has not been an active server connection for an extended period of time. Stopping connection.");
                connection.Stop(new TimeoutException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ReconnectWindowTimeout,
                    connection.LastActiveAt, connection.ReconnectWindow)));

                return false;
            }

            return true;
        }

        private static void UpdateGroups(IConnection connection, JToken groupsToken)
        {
            if (groupsToken != null)
            {
                connection.GroupsToken = (string)groupsToken;
            }
        }

        private static void TryInitialize(JToken response, Action onInitialized)
        {
            if ((int?)response["S"] == 1)
            {
                onInitialized();
            }
        }
    }
}
