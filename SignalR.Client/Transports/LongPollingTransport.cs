using System;
using System.IO;
using System.Threading;
using SignalR.Client.Http;

namespace SignalR.Client.Transports
{
    public class LongPollingTransport : HttpBasedTransport
    {
        private static readonly TimeSpan _errorDelay = TimeSpan.FromSeconds(2);

        public TimeSpan ReconnectDelay { get; set; }

        public LongPollingTransport()
            : this(new DefaultHttpClient())
        {
        }

        public LongPollingTransport(IHttpClient httpClient)
            : base(httpClient, "longPolling")
        {
            ReconnectDelay = TimeSpan.FromSeconds(5);
        }

        protected override void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            PollingLoop(connection, data, initializeCallback, errorCallback);
        }

        private void PollingLoop(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback, bool raiseReconnect = false)
        {
            string url = connection.Url;
            var reconnectTokenSource = new CancellationTokenSource();
            int reconnectFired = 0;

            if (connection.MessageId == null)
            {
                url += "connect";
            }
            else if (raiseReconnect)
            {
                url += "reconnect";
            }

            url += GetReceiveQueryString(connection, data);

            _httpClient.PostAsync(url, PrepareRequest(connection)).ContinueWith(task =>
            {
                // Clear the pending request
                connection.Items.Remove(HttpRequestKey);

                bool shouldRaiseReconnect = false;
                bool disconnectedReceived = false;

                try
                {
                    if (!task.IsFaulted)
                    {
                        if (raiseReconnect)
                        {
                            // If the timeout for the reconnect hasn't fired as yet just fire the 
                            // event here before any incoming messages are processed
                            FireReconnected(connection, reconnectTokenSource, ref reconnectFired);
                        }

                        // Get the response
                        var raw = task.Result.ReadAsString();

                        ProcessResponse(connection, raw, out shouldRaiseReconnect, out disconnectedReceived);
                    }
                }
                finally
                {
                    if (disconnectedReceived)
                    {
                        connection.Stop();
                    }
                    else
                    {
                        bool requestAborted = false;

                        if (task.IsFaulted)
                        {
                            // Cancel the previous reconnect event
                            reconnectTokenSource.Cancel();

                            // Raise the reconnect event if we successfully reconnect after failing
                            shouldRaiseReconnect = true;

                            // Get the underlying exception
                            Exception exception = task.Exception.GetBaseException();

                            // If the error callback isn't null then raise it and don't continue polling
                            if (errorCallback != null)
                            {
                                // Raise on error
                                connection.OnError(exception);

                                // Call the callback
                                errorCallback(exception);
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
                                    TaskAsyncHelper.Delay(_errorDelay).Then(() =>
                                    {
                                        if (connection.IsActive)
                                        {
                                            PollingLoop(connection,
                                                data,
                                                initializeCallback: null,
                                                errorCallback: null,
                                                raiseReconnect: shouldRaiseReconnect);
                                        }
                                    });
                                }
                            }
                        }
                        else
                        {
                            if (connection.IsActive)
                            {
                                // Continue polling if there was no error
                                PollingLoop(connection,
                                            data,
                                            initializeCallback: null,
                                            errorCallback: null,
                                            raiseReconnect: shouldRaiseReconnect);
                            }
                        }
                    }
                }
            });

            if (initializeCallback != null)
            {
                initializeCallback();
            }

            if (raiseReconnect)
            {
                TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
                {
                    // Fire the reconnect event after the delay. This gives the 
                    FireReconnected(connection, reconnectTokenSource, ref reconnectFired);
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void FireReconnected(IConnection connection, CancellationTokenSource reconnectTokenSource, ref int reconnectedFired)
        {
            if (!reconnectTokenSource.IsCancellationRequested)
            {
                if (Interlocked.Exchange(ref reconnectedFired, 1) == 0)
                {
                    connection.OnReconnected();
                }
            }
        }
    }
}
