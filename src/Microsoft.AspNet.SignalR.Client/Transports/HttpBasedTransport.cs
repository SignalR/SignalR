// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The send query string
        private const string _sendQueryString = "?transport={0}&connectionId={1}{2}";

        // The transport name
        private readonly string _transport;

        private readonly IHttpClient _httpClient;

        protected HttpBasedTransport(IHttpClient httpClient, string transport)
        {
            _httpClient = httpClient;
            _transport = transport;
        }

        protected IHttpClient HttpClient
        {
            get { return _httpClient; }
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

        public Task Start(IConnection connection, string data, CancellationToken disconnectToken, Action end)
        {
            var tcs = new TaskCompletionSource<object>();

            OnStart(connection, data, disconnectToken, end, () => tcs.TrySetResult(null), exception => tcs.TrySetException(exception));

            return tcs.Task;
        }

        protected abstract void OnStart(IConnection connection,
                                        string data,
                                        CancellationToken disconnectToken,
                                        Action end,
                                        Action initializeCallback,
                                        Action<Exception> errorCallback);

        public Task<T> Send<T>(IConnection connection, string data)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            string url = connection.Url + "send";
            string customQueryString = GetCustomQueryString(connection);

            url += String.Format(CultureInfo.InvariantCulture, _sendQueryString, _transport, connection.ConnectionId, customQueryString);

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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want Stop to throw. IHttpClient.PostAsync could throw anything.")]
        public void Abort(IConnection connection)
        {
            string url = connection.Url + "abort" + String.Format(CultureInfo.InvariantCulture, _sendQueryString, _transport, connection.ConnectionId, null);

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

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called by internally")]
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

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is used on Silverlight and Windows Phone")]
        private static string GetNoCacheUrlParam()
        {
            return "noCache=" + Guid.NewGuid().ToString();
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is called internally.")]
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification = "This is called internally.")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The client receives the exception in the OnError callback.")]
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

                UpdateGroups(connection,
                             resetGroups: result["R"],
                             addedGroups: result["G"],
                             removedGroups: result["g"]);

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

        private static void UpdateGroups(IConnection connection,
                                         IEnumerable<JToken> resetGroups,
                                         IEnumerable<JToken> addedGroups,
                                         IEnumerable<JToken> removedGroups)
        {
            if (resetGroups != null)
            {
                connection.Groups.Clear();
                EnumerateJTokens(resetGroups, connection.Groups.Add);
            }
            else
            {
                EnumerateJTokens(addedGroups, connection.Groups.Add);
                EnumerateJTokens(removedGroups, g => connection.Groups.Remove(g));
            }
        }

        private static void EnumerateJTokens(IEnumerable<JToken> items, Action<string> process)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    process(item.ToString());
                }
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
