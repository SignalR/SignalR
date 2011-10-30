using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.Client.Transports
{
    public class LongPollingTransport : IClientTransport
    {
        public void Start(Connection connection, string data)
        {
            string url = connection.Url;

            if (connection.MessageId == null)
            {
                url += "connect";
            }

            var parameters = new Dictionary<string, string> {
                { "data", data },
                { "messageId", Convert.ToString(connection.MessageId) },
                { "clientId", connection.ClientId },
                { "transport", "longPolling" },
                { "groups", String.Join(",", connection.Groups.ToArray()) }
            };

            HttpHelper.PostAsync(url, parameters).ContinueWith(task =>
            {
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
                    if (task.IsFaulted)
                    {
                        connection.OnError(task.Exception.GetBaseException());

                        // If we can recover from this exception then sleep for 2 seconds
                        if (CanRecover(task.Exception))
                        {
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            // If we couldn't recover then we need to stop the connection
                            connection.Stop();
                        }
                    }

                    // Only continue if the connection is still active
                    if (connection.IsActive)
                    {
                        Start(connection, data);
                    }
                }
            });
        }

        public Task<T> Send<T>(Connection connection, string data)
        {
            string url = connection.Url + "send";

            var postData = new Dictionary<string, string> {
                { "data", data },
                { "clientId", connection.ClientId },
                { "transport" , "longPolling" }
            };

            return HttpHelper.PostAsync(url, postData).Success(task =>
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
        }

        private bool CanRecover(Exception exception)
        {
            var webException = exception.GetBaseException() as WebException;
            if (webException != null)
            {
                var httpResponse = (HttpWebResponse)webException.Response;
                if (httpResponse != null &&
                    httpResponse.StatusCode != HttpStatusCode.InternalServerError)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ProcessResponse(Connection connection, string response)
        {
            if (connection.MessageId == null)
            {
                connection.MessageId = 0;
            }

            try
            {
                JObject result = JObject.Parse(response);
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
