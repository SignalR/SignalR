using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalR.Client.Http;
#if NET20
using SignalR.Client.Net20.Infrastructure;
using Newtonsoft.Json.Serialization;
#else
using System.Linq;
using System.Threading.Tasks;
#endif

namespace SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The receive query string
        private const string _receiveQueryString = "?transport={0}&connectionId={1}&messageId={2}&groups={3}&connectionData={4}{5}";

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
            string negotiateUrl = connection.Url + "negotiate";

#if NET20
            return httpClient.GetAsync(negotiateUrl, connection.PrepareRequest).FollowedBy(response =>
#else
            return httpClient.GetAsync(negotiateUrl, connection.PrepareRequest).Then(response =>
#endif
            {
                string raw = response.ReadAsString();

                if (raw == null)
                {
                    throw new InvalidOperationException("Server negotiation failed.");
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

#if NET20
            return _httpClient.PostAsync(url, connection.PrepareRequest, postData).FollowedBy(response =>
#else
            return _httpClient.PostAsync(url, connection.PrepareRequest, postData).Then(response =>
#endif
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
            return String.Format(_receiveQueryString,
                                 _transport,
                                 Uri.EscapeDataString(connection.ConnectionId),
                                 Convert.ToString(connection.MessageId),
                                 Uri.EscapeDataString(JsonConvert.SerializeObject(connection.Groups)),
                                 data,
                                 GetCustomQueryString(connection));
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

        protected static bool IsRequestAborted(Exception exception)
        {
            var webException = exception as WebException;
            return (webException != null && webException.Status == WebExceptionStatus.RequestCanceled);
        }

        public void Stop(IConnection connection)
        {
#if NET20
            var httpRequest = ConnectionExtensions.GetValue<IRequest>(connection, HttpRequestKey);
#else
            var httpRequest = connection.GetValue<IRequest>(HttpRequestKey);
#endif
            if (httpRequest != null)
            {
                try
                {
                    OnBeforeAbort(connection);
                    httpRequest.Abort();
                }
                catch (NotImplementedException)
                {
                    // If this isn't implemented then do nothing
                }
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

            if (connection.MessageId == null)
            {
                connection.MessageId = 0;
            }

            try
            {
                var result = JToken.Parse(response);

                if (!result.HasValues)
                {
                    return;
                }

                timedOut = result.Value<bool>("TimedOut");
                disconnected = result.Value<bool>("Disconnect");

                if (disconnected)
                {
                    return;
                }

                var messages = result["Messages"] as JArray;
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
#if NET20
                            Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to process message: {0}", ex));
#else
                            Debug.WriteLine("Failed to process message: {0}", ex);
#endif
                            connection.OnError(ex);
                        }
                    }

                    connection.MessageId = result["MessageId"].Value<long>();

                    var transportData = result["TransportData"] as JObject;

                    if (transportData != null)
                    {
                        var groups = (JArray)transportData["Groups"];
                        if (groups != null)
                        {
#if NET20
                            var groupList = new List<string>();
                            foreach (var groupFromTransport in groups)
                            {
                                groupList.Add(groupFromTransport.Value<string>());
                            }
                            connection.Groups = groupList;
#else
                            connection.Groups = groups.Select(token => token.Value<string>());
#endif
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if NET20
                Debug.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to response: {0}", ex));
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
