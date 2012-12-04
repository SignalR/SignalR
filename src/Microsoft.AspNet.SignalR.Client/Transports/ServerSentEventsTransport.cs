// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class ServerSentEventsTransport : HttpBasedTransport
    {
        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents")
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

        protected override void OnStart(IConnection connection,
                                        string data,
                                        CancellationToken disconnectToken,
                                        Action end,
                                        Action initializeCallback,
                                        Action<Exception> errorCallback)
        {
            var disconnectInvoker = new ThreadSafeInvoker();
            OpenConnection(connection, data, disconnectToken,
                           () => disconnectInvoker.Invoke(end),
                           initializeCallback,
                           errorCallback);
        }

        private void Reconnect(IConnection connection, string data, CancellationToken disconnectToken, Action end)
        {
            // Wait for a bit before reconnecting
            TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
            {
                if (connection.EnsureReconnecting())
                {
                    // Now attempt a reconnect
                    OpenConnection(connection, data,  disconnectToken, end, initializeCallback: null, errorCallback: null);
                }
            });
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "We will refactor later.")]
        private void OpenConnection(IConnection connection,
                                    string data,
                                    CancellationToken disconnectToken,
                                    Action end,
                                    Action initializeCallback,
                                    Action<Exception> errorCallback)
        {
            if (disconnectToken.IsCancellationRequested)
            {
                if (errorCallback != null)
                {
#if NET35
                    errorCallback(new OperationCanceledException(Resources.Error_ConnectionCancelled));
#else
                    errorCallback(new OperationCanceledException(Resources.Error_ConnectionCancelled, disconnectToken));
#endif
                }

                end();
                return;
            }

            // If we're reconnecting add /connect to the url
            bool reconnecting = initializeCallback == null;
            var callbackInvoker = new ThreadSafeInvoker();

            var url = (reconnecting ? connection.Url : connection.Url + "connect") + GetReceiveQueryString(connection, data);
            IRequest request = null;

#if NET35
            Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "SSE: GET {0}", (object)url));
#else
            Debug.WriteLine("SSE: GET {0}", (object)url);
#endif

            HttpClient.GetAsync(url, req =>
            {
                request = req;
                connection.PrepareRequest(request);

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

                            Reconnect(connection, data, disconnectToken, end);
                        }
                    }
                }
                else
                {
                    IResponse response = task.Result;
                    Stream stream = response.GetResponseStream();

                    var eventSource = new EventSourceStreamReader(stream);
                    bool retry = true;

                    disconnectToken.SafeRegister(es =>
                    {
                        retry = false;
                        es.Close();
                        response.Close();
                    }, eventSource);

                    eventSource.Opened = () =>
                    {
                        if (!reconnecting)
                        {
                            callbackInvoker.Invoke(initializeCallback);
                        }
                        else if (connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
                        {
                            // Raise the reconnect event if the connection comes back up
                            connection.OnReconnected();
                        }
                    };

                    eventSource.Message = sseEvent =>
                    {
                        if (sseEvent.EventType == EventType.Data)
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
                        if (!isRequestAborted && retry)
                        {
                            Reconnect(connection, data, disconnectToken, end);
                        }
                        else
                        {
                            connection.Stop();
                        }
                    };
                    eventSource.Start();
                }
                disconnectToken.SafeRegister(ecb =>
                {
                    if (ecb != null) {
                        callbackInvoker.Invoke((cb, token) =>
                        {
#if NET35
                            cb(new OperationCanceledException(Resources.Error_ConnectionCancelled));
#else
                            cb(new OperationCanceledException(Resources.Error_ConnectionCancelled, token));
#endif
                        }, ecb, disconnectToken);
                    }

                    if (request != null)
                    {
                        request.Abort();
                    }
                    end();
                }, errorCallback);
            });

            if (errorCallback != null)
            {
                TaskAsyncHelper.Delay(ConnectionTimeout).Then(() =>
                {
                    callbackInvoker.Invoke((conn, cb) =>
                    {
                        // Stop the connection
                        disconnectToken.SafeRegister(e => e(), end);
                        connection.Stop();

                        // Connection timeout occurred
                        cb(new TimeoutException());
                    },
                    connection,
                    errorCallback);
                });
            }
        }
    }
}
