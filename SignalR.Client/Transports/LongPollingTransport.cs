using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.Client.Transports
{
    public class LongPollingTransport : IClientTransport
    {
        private HttpWebRequest _pollingRequest;
        private readonly object _lockObj = new object();
        private const string _receiveQs = "?transport=longPolling&connectionId={0}&messageId={1}&groups={2}&connectionData={3}";
        private const string _sendQs = "?transport=longPolling&connectionId={0}";

        public void Start(Connection connection, string data)
        {
            string url = connection.Url;

            if (connection.MessageId == null)
            {
                url += "connect";
            }

            url += String.Format(_receiveQs,
                                 Uri.EscapeDataString(connection.ConnectionId),
                                 Convert.ToString(connection.MessageId),
                                 Uri.EscapeDataString(String.Join(",", connection.Groups.ToArray())),
                                 data);

            Action<HttpWebRequest> prepareRequest = request =>
            {
                // Setup the user agent along with any other defaults
                connection.PrepareRequest(request);

                lock (_lockObj)
                {
                    // Keep track of the pending request just in case we need to cancel it.
                    _pollingRequest = request;
                }
            };

            HttpHelper.PostAsync(url, prepareRequest).ContinueWith(task =>
            {
                lock (_lockObj)
                {
                    // Clear the pending request
                    _pollingRequest = null;
                }

                try
                {
                    if (!task.IsFaulted)
                    {
                        // Get the response
                        var raw = task.Result.ReadAsString();

                        if (!String.IsNullOrEmpty(raw))
                        {
                            ProcessResponse(connection, raw);
                        }
                    }
                }
                finally
                {
                    bool requestAborted = false;

                    if (task.IsFaulted)
                    {
                        // Get the underlying exception
                        Exception exception = task.Exception.GetBaseException();

                        // Figure out if the request was aborted
                        requestAborted = IsRequestAborted(exception);

                        // Sometimes a connection might have been closed by the server before we get to write anything
                        // so just try again and don't raise OnError.
                        if (!requestAborted && !(exception is IOException))
                        {
                            // Raise on error
                            connection.OnError(exception);

                            // If the connection is still active after raising the error event wait for 2 seconds
                            // before polling again so we aren't hammering the server
                            if (connection.IsActive)
                            {
                                Thread.Sleep(2000);
                            }
                        }
                    }

                    // Only continue if the connection is still active and wasn't aborted
                    if (!requestAborted && connection.IsActive)
                    {
                        Start(connection, data);
                    }
                }
            });
        }

        private static bool IsRequestAborted(Exception exception)
        {
            var webException = exception as WebException;
            return (webException != null && webException.Status == WebExceptionStatus.RequestCanceled);
        }

        public Task<T> Send<T>(Connection connection, string data)
        {
            string url = connection.Url + "send";

            url += String.Format(_sendQs, connection.ConnectionId);

            var postData = new Dictionary<string, string> {
                { "data", data }
            };

            return HttpHelper.PostAsync(url, connection.PrepareRequest, postData).Success(task =>
            {
                string raw = task.Result.ReadAsString();

                if (String.IsNullOrEmpty(raw))
                {
                    return default(T);
                }

                return JsonConvert.DeserializeObject<T>(raw);
            });
        }

        public void Stop(Connection connection)
        {
            if (_pollingRequest != null)
            {
                lock (_lockObj)
                {
                    if (_pollingRequest != null)
                    {
                        try
                        {
                            _pollingRequest.Abort();
                            _pollingRequest = null;
                        }
                        catch (NotImplementedException)
                        {
                            // If this isn't implemented then do nothing
                        }
                    }
                }
            }
        }

        private static void ProcessResponse(Connection connection, string response)
        {
            if (connection.MessageId == null)
            {
                connection.MessageId = 0;
            }

            try
            {
                var result = JValue.Parse(response);

                if (!result.HasValues)
                {
                    return;
                }

                var messages = result["Messages"] as JArray;

                if (messages != null)
                {
                    foreach (var message in messages)
                    {
                        try
                        {
                            connection.OnReceived(message.ToString());
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Failed to process message: {0}", ex);
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
                            connection.Groups = groups.Select(token => token.Value<string>());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to response: {0}", ex);
                connection.OnError(ex);
            }
        }
    }
}
