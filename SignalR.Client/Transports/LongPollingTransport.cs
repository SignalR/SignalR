using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SignalR.Client.Http;
using SignalR.Client.Infrastructure;

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

            // This is only necessary for the initial request where initializeCallback and errorCallback are non-null
            int callbackFired = 0;

            if (connection.MessageId == null)
            {
                url += "connect";
            }
            else if (raiseReconnect)
            {
                url += "reconnect";

                connection.State = ConnectionState.Reconnecting;
            }

            url += GetReceiveQueryString(connection, data);

            Debug.WriteLine("LP: {0}", (object)url);

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

                        Debug.WriteLine("LP Receive: {0}", (object)raw);

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
                            Exception exception = task.Exception.Unwrap();

                            // If the error callback isn't null then raise it and don't continue polling
                            if (errorCallback != null && 
                                Interlocked.Exchange(ref callbackFired, 1) == 0)
                            {
                                // Call the callback
                                errorCallback(exception);
                            }
                            else
                            {
                                // Figure out if the request was aborted
                                requestAborted = ExceptionHelper.IsRequestAborted(exception);

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
                                        if (!CancellationToken.IsCancellationRequested)
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
                            if (!CancellationToken.IsCancellationRequested)
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
                if (Interlocked.Exchange(ref callbackFired, 1) == 0)
                {
                    initializeCallback();
                }
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
                    // Mark the connection as connected
                    connection.State = ConnectionState.Connected;

                    connection.OnReconnected();
                }
            }
        }
    }
}
