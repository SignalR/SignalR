﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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


            return httpClient.Get(negotiateUrl, connection.PrepareRequest).Then(response =>
            {
                string raw = response.ReadAsString();

                if (raw == null)
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
                qsBuilder.Append("&")
                         .Append(customQuery);
            }

#if SILVERLIGHT || WINDOWS_PHONE
            qsBuilder.Append("&").Append(GetNoCacheUrlParam());
#endif
            return qsBuilder.ToString();
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called internally.")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "This is called internally.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The client receives the exception in the OnError callback.")]
        public static void ProcessResponse(IConnection connection, string response, out bool timedOut, out bool disconnected)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            // Update the LastKeepAlive Value
            if (connection.KeepAliveData != null)
            {
                connection.UpdateLastKeepAlive();
                Debug.WriteLine("Received Message from the Server : {0}", DateTime.UtcNow);
            }

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

                timedOut = result.Value<int>("T") == 1;
                disconnected = result.Value<int>("D") == 1;

                if (disconnected)
                {
                    return;
                }

                UpdateGroups(connection, groupsToken: result["G"]);

                var messages = result["M"] as JArray;
                if (messages != null)
                {
                    foreach (JToken message in messages)
                    {
                        try
                        {
                            connection.OnReceived(message);
                        }
                        catch (Exception ex)
                        {
#if NET35
                                Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "Failed to process message: {0}", ex));
#else
                            Debug.WriteLine("Failed to process message: {0}", ex);
#endif

                            connection.OnError(ex);
                        }
                    }

                    connection.MessageId = result["C"].Value<string>();
                }
            }
            catch (Exception ex)
            {
#if NET35
                    Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "Failed to response: {0}", ex));
#else
                Debug.WriteLine("Failed to response: {0}", ex);
#endif
                connection.OnError(ex);
            }
        }


        private static void UpdateGroups(IConnection connection, JToken groupsToken)
        {
            if (groupsToken != null)
            {
                connection.GroupsToken = groupsToken.Value<string>();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is used on Silverlight and Windows Phone")]
        private static string GetNoCacheUrlParam()
        {
            return "noCache=" + Guid.NewGuid().ToString();
        }
    }
}
