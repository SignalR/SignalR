// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public static class TransportHelper
    {
        public static Task<NegotiationResponse> GetNegotiationResponse(this IHttpClient httpClient, IConnection connection)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

#if SILVERLIGHT || WINDOWS_PHONE
            string negotiateUrl = connection.Url + "negotiate?" + GetNoCacheUrlParam();
#else
            string negotiateUrl = connection.Url + "negotiate";
#endif
            negotiateUrl += AppendCustomQueryString(connection, negotiateUrl);

            char appender = '?';
            if (negotiateUrl.Contains("?"))
            {
                appender = '&';
            }

            negotiateUrl += appender + "clientProtocol=" + connection.Protocol;

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

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by internally")]
        public static string GetReceiveQueryString(IConnection connection, string data, string transport)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            // ?transport={0}&connectionToken={1}&messageId={2}&groups={3}&connectionData={4}{5}
            var qsBuilder = new StringBuilder();
            qsBuilder.Append("?transport=" + transport)
                     .Append("&connectionToken=" + Uri.EscapeDataString(connection.ConnectionToken));

            if (connection.MessageId != null)
            {
                qsBuilder.Append("&messageId=" + Uri.EscapeDataString(connection.MessageId));
            }

            if (connection.GroupsToken != null)
            {
                qsBuilder.Append("&groupsToken=" + Uri.EscapeDataString(connection.GroupsToken));
            }

            if (data != null)
            {
                qsBuilder.Append("&connectionData=" + data);
            }

            string customQuery = connection.QueryString;

            if (!String.IsNullOrEmpty(customQuery))
            {
                qsBuilder.Append("&").Append(customQuery);
            }

#if SILVERLIGHT || WINDOWS_PHONE
            qsBuilder.Append("&").Append(GetNoCacheUrlParam());
#endif
            return qsBuilder.ToString();
        }

        public static string AppendCustomQueryString(IConnection connection, string baseUrl)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (baseUrl == null)
            {
                baseUrl = "";
            }

            string appender = "",
                   customQuery = connection.QueryString,
                   qs = "";

            if (!String.IsNullOrEmpty(customQuery))
            {
                char firstChar = customQuery[0];

                // If the custom query string already starts with an ampersand or question mark
                // then we dont have to use any appender, it can be empty.
                if (firstChar != '?' && firstChar != '&')
                {
                    appender = "?";

                    if (baseUrl.Contains(appender))
                    {
                        appender = "&";
                    }
                }

                qs += appender + customQuery;
            }

            return qs;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification="This is called internally.")]
        public static void ProcessResponse(IConnection connection, string response, out bool timedOut, out bool disconnected)
        {
            ProcessResponse(connection, response, out timedOut, out disconnected, () => { });
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called internally.")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "This is called internally.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The client receives the exception in the OnError callback.")]
        public static void ProcessResponse(IConnection connection, string response, out bool timedOut, out bool disconnected, Action onInitialized)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            connection.UpdateLastKeepAlive();

            timedOut = false;
            disconnected = false;

            if (String.IsNullOrEmpty(response))
            {
                return;
            }

            try
            {
                var result = JValue.Parse(response);

                if (!result.HasValues)
                {
                    return;
                }

                if (result["I"] != null)
                {
                    connection.OnReceived(result);
                    return;
                }

                timedOut = (int?)result["T"] == 1;
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

                    foreach (JToken message in messages)
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

        private static void UpdateGroups(IConnection connection, JToken groupsToken)
        {
            if (groupsToken != null)
            {
                connection.GroupsToken = (string)groupsToken;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is used on Silverlight and Windows Phone")]
        private static string GetNoCacheUrlParam()
        {
            return "noCache=" + Guid.NewGuid().ToString();
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
