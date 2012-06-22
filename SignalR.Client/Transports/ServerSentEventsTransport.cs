using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SignalR.Client.Http;
using SignalR.Client.Infrastructure;
using SignalR.Client.Transports.ServerSentEvents;

namespace SignalR.Client.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        private int _initializedCalled;

        private const string EventSourceKey = "eventSourceStream";
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
            // Wait for a bit before reconnecting
            TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
            {
                if (connection.ChangeState(ConnectionState.Connected, ConnectionState.Reconnecting))
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
                        // Retry
                        Reconnect(connection, data);
                        return;
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
                        if (Interlocked.CompareExchange(ref _initializedCalled, 1, 0) == 0)
                        {
                            initializeCallback();
                        }

                        if (reconnecting && connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
                        {
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
