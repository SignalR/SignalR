using System;
using System.IO;
using System.Threading;

namespace SignalR.Client.Transports
{
    public class LongPollingTransport : HttpBasedTransport
    {
        public LongPollingTransport()
            : base("longPolling")
        {
        }

        protected override void OnStart(Connection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            PollingLoop(connection, data, initializeCallback, errorCallback);
        }

        private void PollingLoop(Connection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            string url = connection.Url;

            if (connection.MessageId == null)
            {
                url += "connect";
            }

            url += GetReceiveQueryString(connection, data);

            HttpHelper.PostAsync(url, PrepareRequest(connection)).ContinueWith(task =>
            {
                // Clear the pending request
                connection.Items.Remove(HttpRequestKey);

                try
                {
                    if (!task.IsFaulted)
                    {
                        // Get the response
                        var raw = task.Result.ReadAsString();

                        if (!String.IsNullOrEmpty(raw))
                        {
                            OnMessage(connection, raw);
                        }
                    }
                }
                finally
                {
                    bool requestAborted = false;
                    bool continuePolling = true;

                    if (task.IsFaulted)
                    {
                        // Get the underlying exception
                        Exception exception = task.Exception.GetBaseException();

                        // If the error callback isn't null then raise it and don't continue polling
                        if (errorCallback != null)
                        {
                            // Raise on error
                            connection.OnError(exception);

                            // Call the callback
                            errorCallback(exception);

                            // Don't continue polling if the error is on the first request
                            continuePolling = false;
                        }
                        else
                        {
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
                    }

                    // Only continue if the connection is still active and wasn't aborted
                    if (continuePolling && !requestAborted && connection.IsActive)
                    {
                        PollingLoop(connection, data, null, null);
                    }
                }
            });

            if (initializeCallback != null)
            {
                // Only set this the first time
                // TODO: We should delay this until after the http request has been made
                initializeCallback();
            }
        }
    }
}
