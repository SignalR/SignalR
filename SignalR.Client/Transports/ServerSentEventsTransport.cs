using System;
using System.Diagnostics;
using System.IO;
using SignalR.Client.Http;
using SignalR.Client.Infrastructure;
using SignalR.Client.Transports.ServerSentEvents;

namespace SignalR.Client.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        private int _initializedCalled;

        private const string EventSourceKey = "eventSourceStream";

        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents", true)
        {
            ReconnectDelay = TimeSpan.FromSeconds(2);
            ConnectionTimeout = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Time allowed before failing the connect request.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        protected override void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            OpenConnection(connection, data, initializeCallback, errorCallback);
        }

        public override void LostConnection(IConnection connection)
        {
            // Stopping the transport will force it into auto reconnect and maintain the functional flow
            Stop(connection, false);
        }

        private void Reconnect(IConnection connection, string data)
        {
            // Wait for a bit before reconnecting
            TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
            {
                if (connection.State == ConnectionState.Reconnecting ||
                    connection.ChangeState(ConnectionState.Connected, ConnectionState.Reconnecting))
                {
                    // Now attempt a reconnect
                    OpenConnection(connection, data, initializeCallback: null, errorCallback: null);
                }
            });
        }

        private void OpenConnection(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback)
        {
            // If we're reconnecting add /connect to the url
            bool reconnecting = initializeCallback == null;
            var callbackInvoker = new ThreadSafeInvoker();

            var url = (reconnecting ? connection.Url : connection.Url + "connect") + GetReceiveQueryString(connection, data);

            Action<IRequest> prepareRequest = PrepareRequest(connection);

#if NET35
            Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "SSE: GET {0}", (object)url));
#else
            Debug.WriteLine("SSE: GET {0}", (object)url);
#endif

            _httpClient.GetAsync(url, request =>
            {
                prepareRequest(request);

                request.Accept = "text/event-stream";

            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Exception exception = task.Exception.Unwrap();
                    if (!ExceptionHelper.IsRequestAborted(exception))
                    {
                        if (errorCallback != null)
                        {
                            callbackInvoker.Invoke((cb, ex) => cb(ex), errorCallback, exception);
                        }
                        else if (reconnecting)
                        {
                            // Only raise the error event if we failed to reconnect
                            connection.OnError(exception);

                            Reconnect(connection, data);
                        }
                    }
                }
                else
                {
                    IResponse response = task.Result;
                    Stream stream = response.GetResponseStream();

                    var eventSource = new EventSourceStreamReader(stream);
                    bool retry = true;

                    connection.Items[EventSourceKey] = eventSource;

                    eventSource.Opened = () =>
                    {
                        if (initializeCallback != null)
                        {
                            callbackInvoker.Invoke(initializeCallback);
                        }

                        if (reconnecting && connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
                        {
                            // Raise the reconnect event if the connection comes back up
                            connection.OnReconnected();
                        }
                    };

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

                            UpdateKeepAlive();
                            ProcessResponse(connection, sseEvent.Data, out timedOut, out disconnected);

                            if (disconnected)
                            {
                                retry = false;
                            }
                        }
                    };

                    eventSource.Closed = exception =>
                    {
                        bool isRequestAborted = false;

                        if (exception != null)
                        {
                            // Check if the request is aborted
                            isRequestAborted = ExceptionHelper.IsRequestAborted(exception);

                            if (!isRequestAborted)
                            {
                                // Don't raise exceptions if the request was aborted (connection was stopped).
                                connection.OnError(exception);
                            }
                        }

                        // See http://msdn.microsoft.com/en-us/library/system.net.httpwebresponse.close.aspx
                        response.Close();

                        // Skip reconnect attempt for aborted requests
                        if (!isRequestAborted)
                        {
                            if (retry)
                            {
                                Reconnect(connection, data);
                            }
                            else
                            {
                                connection.Stop();
                            }
                        }                        
                    };

                    eventSource.Start();
                }
            });

            if (errorCallback != null)
            {
                TaskAsyncHelper.Delay(ConnectionTimeout).Then(() =>
                {
                    callbackInvoker.Invoke((conn, cb) =>
                    {
                        // Stop the connection
                        Stop(conn);

                        // Connection timeout occured
                        cb(new TimeoutException());
                    },
                    connection,
                    errorCallback);
                });
            }
        }

        /// <summary>
        /// Stops even event source as well and the base connection.
        /// </summary>
        /// <param name="connection">The <see cref="IConnection"/> being aborted.</param>
        protected override void OnBeforeAbort(IConnection connection)
        {
            var eventSourceStream = connection.GetValue<EventSourceStreamReader>(EventSourceKey);
            if (eventSourceStream != null)
            {
                eventSourceStream.Close();
            }

            base.OnBeforeAbort(connection);
        }
    }
}
