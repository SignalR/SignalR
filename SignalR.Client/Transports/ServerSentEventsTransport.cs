using System;
using System.Diagnostics;
using System.Threading;
using SignalR.Client.Http;
using SignalR.Client.Infrastructure;
using SignalR.Client.Transports.ServerSentEvents;

namespace SignalR.Client.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        private int _initializedCalled;

        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(2);

        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents")
        {
            ConnectionTimeout = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Time allowed before failing the connect request
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        protected override void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            OpenConnection(connection, data, initializeCallback, errorCallback);
        }

        private void Reconnect(IConnection connection, string data)
        {
            if (connection.IsDisconnecting())
            {
                return;
            }

            // Wait for a bit before reconnecting
            TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
            {
                // Now attempt a reconnect
                OpenConnection(connection, data, initializeCallback: null, errorCallback: null);
            });
        }

        private void OpenConnection(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            // If we're reconnecting add /connect to the url
            bool reconnecting = initializeCallback == null;

            var url = (reconnecting ? connection.Url : connection.Url + "connect") + GetReceiveQueryString(connection, data);

            Action<IRequest> prepareRequest = PrepareRequest(connection);

            Debug.WriteLine("SSE: GET {0}", (object)url);

            _httpClient.GetAsync(url, request =>
            {
                prepareRequest(request);

                request.Accept = "text/event-stream";

            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    var exception = task.Exception.Unwrap();
                    if (!ExceptionHelper.IsRequestAborted(exception))
                    {
                        if (errorCallback != null &&
                            Interlocked.Exchange(ref _initializedCalled, 1) == 0)
                        {
                            errorCallback(exception);
                        }
                        else if (reconnecting)
                        {
                            // Only raise the error event if we failed to reconnect
                            connection.OnError(exception);
                        }
                    }

                    if (reconnecting)
                    {
                        connection.State = ConnectionState.Reconnecting;

                        // Retry
                        Reconnect(connection, data);
                        return;
                    }
                }
                else
                {
                    // Get the reseponse stream and read it for messages
                    var response = task.Result;
                    var stream = response.GetResponseStream();

                    var eventSource = new EventSourceStreamReader(stream);
                    bool retry = true;

                    eventSource.Opened = () =>
                    {
                        if (Interlocked.CompareExchange(ref _initializedCalled, 1, 0) == 0)
                        {
                            initializeCallback();
                        }

                        if (reconnecting)
                        {
                            // Change the status to connected
                            connection.State = ConnectionState.Connected;

                            // Raise the reconnect event if the connection comes back up
                            connection.OnReconnected();
                        }
                    };

                    eventSource.Error = connection.OnError;

                    eventSource.Message = sseEvent =>
                    {
                        if (sseEvent.Type == EventType.Data)
                        {
                            if (sseEvent.Data.Equals("initialized", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }

                            bool timedOut;
                            bool disconnected;
                            ProcessResponse(connection, sseEvent.Data, out timedOut, out disconnected);

                            if (disconnected)
                            {
                                retry = false;
                            }
                        }
                    };

                    eventSource.Closed = () =>
                    {
                        response.Close();

                        if (retry)
                        {
                            // If we're retrying then just go again
                            connection.State = ConnectionState.Reconnecting;

                            Reconnect(connection, data);
                        }
                        else
                        {
                            connection.Stop();
                        }
                    };

                    eventSource.Start();
                }
            });

            if (initializeCallback != null)
            {
                TaskAsyncHelper.Delay(ConnectionTimeout).Then(() =>
                {
                    if (Interlocked.CompareExchange(ref _initializedCalled, 1, 0) == 0)
                    {
                        // Stop the connection
                        Stop(connection);

                        // Connection timeout occured
                        errorCallback(new TimeoutException());
                    }
                });
            }
        }
    }
}
