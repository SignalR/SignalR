// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.SignalR.Client.Http;
using System.Globalization;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The send query string
        private const string _sendQueryString = "?transport={0}&connectionId={1}{2}";

        // The transport name
        protected readonly string _transport;

        protected const string HttpRequestKey = "http.Request";

        protected readonly IHttpClient _httpClient;

        public HttpBasedTransport(IHttpClient httpClient, string transport)
        {
            _httpClient = httpClient;
            _transport = transport;
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return GetNegotiationResponse(_httpClient, connection);
        }

        internal static Task<NegotiationResponse> GetNegotiationResponse(IHttpClient httpClient, IConnection connection)
        {
#if SILVERLIGHT || WINDOWS_PHONE
            string negotiateUrl = connection.Url + "negotiate?" + GetNoCacheUrlParam();
#else
            string negotiateUrl = connection.Url + "negotiate";
#endif


            return httpClient.GetAsync(negotiateUrl, connection.PrepareRequest).Then(response =>
            {
                string raw = response.ReadAsString();

                if (raw == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_ServerNegotiationFailed));
                }

                return JsonConvert.DeserializeObject<NegotiationResponse>(raw);
            });
        }

        public Task Start(IConnection connection, string data)
        {
            var tcs = new TaskCompletionSource<object>();

            OnStart(connection, data, () => tcs.TrySetResult(null), exception => tcs.TrySetException(exception));

            return tcs.Task;
        }

        protected abstract void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback);

        public Task<T> Send<T>(IConnection connection, string data)
        {
            string url = connection.Url + "send";
            string customQueryString = GetCustomQueryString(connection);

            url += String.Format(_sendQueryString, _transport, connection.ConnectionId, customQueryString);

            var postData = new Dictionary<string, string> {
                { "data", data }
            };

            return _httpClient.PostAsync(url, connection.PrepareRequest, postData).Then(response =>
            {
                string raw = response.ReadAsString();

                if (String.IsNullOrEmpty(raw))
                {
                    return default(T);
                }

                return JsonConvert.DeserializeObject<T>(raw);
            });
        }

        protected string GetReceiveQueryString(IConnection connection, string data)
        {
            // ?transport={0}&connectionId={1}&messageId={2}&groups={3}&connectionData={4}{5}
            var qsBuilder = new StringBuilder();
            qsBuilder.Append("?transport=" + _transport)
                     .Append("&connectionId=" + Uri.EscapeDataString(connection.ConnectionId));

            if (connection.MessageId != null)
            {
                qsBuilder.Append("&messageId=" + Uri.EscapeDataString(connection.MessageId));
            }

            if (connection.Groups != null && connection.Groups.Any())
            {
                qsBuilder.Append("&groups=" + Uri.EscapeDataString(JsonConvert.SerializeObject(connection.Groups)));
            }

            if (data != null)
            {
                qsBuilder.Append("&connectionData=" + data);
            }

            string customQuery = GetCustomQueryString(connection);

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

        private static string GetNoCacheUrlParam()
        {
            return "noCache=" + Guid.NewGuid().ToString();
        }

        protected virtual Action<IRequest> PrepareRequest(IConnection connection)
        {
            return request =>
            {
                // Setup the user agent along with any other defaults
                connection.PrepareRequest(request);

                connection.Items[HttpRequestKey] = request;
            };
        }

        public void Stop(IConnection connection)
        {
            var httpRequest = connection.GetValue<IRequest>(HttpRequestKey);
            if (httpRequest != null)
            {
                try
                {
                    OnBeforeAbort(connection);

                    // Abort the server side connection
                    AbortConnection(connection);

                    // Now abort the client connection
                    httpRequest.Abort();
                }
                catch (NotImplementedException)
                {
                    // If this isn't implemented then do nothing
                }
            }
        }

        private void AbortConnection(IConnection connection)
        {
            string url = connection.Url + "abort" + String.Format(_sendQueryString, _transport, connection.ConnectionId, null);

            try
            {
                // Attempt to perform a clean disconnect, but only wait 2 seconds
                _httpClient.PostAsync(url, connection.PrepareRequest).Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                // Swallow any exceptions, but log them
                Debug.WriteLine("Clean disconnect failed. " + ex.Unwrap().Message);
            }
        }


        protected virtual void OnBeforeAbort(IConnection connection)
        {

        }

        protected static void ProcessResponse(IConnection connection, string response, out bool timedOut, out bool disconnected)
        {
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

                timedOut = result.Value<int>("T") == 1;
                disconnected = result.Value<int>("D") == 1;

                if (disconnected)
                {
                    return;
                }

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

                    var addedGroups = result["G"];
                    var removedGroups = result["g"];

                    if (addedGroups != null)
                    {
                        foreach (var group in addedGroups)
                        {
                            connection.Groups.Add(group.ToString());
                        }
                    }

                    if (removedGroups != null)
                    {
                        foreach (var group in removedGroups)
                        {
                            connection.Groups.Remove(group.ToString());
                        }
                    }
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

        private static string GetCustomQueryString(IConnection connection)
        {
            return String.IsNullOrEmpty(connection.QueryString)
                            ? ""
                            : "&" + connection.QueryString;
        }
    }
}
