using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                { "connectionData", data },
                { "messageId", Convert.ToString(connection.MessageId) },
                { "clientId", connection.ClientId },
                { "transport", "longPolling" },
                { "groups", String.Join(",", connection.Groups.ToArray()) }
            };

            HttpHelper.PostAsync(url, connection.PrepareRequest, parameters).ContinueWith(task =>
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
                        // Get the underlying exception
                        Exception exception = task.Exception.GetBaseException();

                        // Sometimes a connection might have been closed by the server before we get to write anything
                        // so just try again and don't raise OnError.
                        if (!(exception is IOException))
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
