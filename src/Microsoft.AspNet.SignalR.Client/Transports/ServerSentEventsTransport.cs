// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
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
        private IRequest _request;

        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents")
        {
            ReconnectDelay = TimeSpan.FromSeconds(2);
            ConnectionTimeout = TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Indicates whether or not the transport supports keep alive
        /// </summary>
        public override bool SupportsKeepAlive
        {
            get
            {
                return true;
            }
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
                                        Action initializeCallback,
                                        Action<Exception> errorCallback)
        {
            OpenConnection(connection, data, disconnectToken, initializeCallback, errorCallback);
        }

        private void Reconnect(IConnection connection, string data, CancellationToken disconnectToken)
        {
            // Wait for a bit before reconnecting
            TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
            {
                // FIX: Race if Connection is stopped and completely restarted between checking the token and calling
                //      connection.EnsureReconnecting()
                if (!disconnectToken.IsCancellationRequested && connection.EnsureReconnecting())
                {
                    // Now attempt a reconnect
                    OpenConnection(connection, data, disconnectToken, initializeCallback: null, errorCallback: null);
                }
            });
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "We will refactor later.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "We will refactor later.")]
        private void OpenConnection(IConnection connection,
                                    string data,
                                    CancellationToken disconnectToken,
                                    Action initializeCallback,
                                    Action<Exception> errorCallback)
        {
            // If we're reconnecting add /connect to the url
            bool reconnecting = initializeCallback == null;
            var callbackInvoker = new ThreadSafeInvoker();
            var requestDisposer = new Disposer();

            var url = connection.Url + (reconnecting ? "reconnect" : "connect") + GetReceiveQueryString(connection, data);

            connection.Trace(TraceLevels.Events, "SSE: GET {0}", url);

            HttpClient.Get(url, req =>
            {
                _request = req;               
                _request.Accept = "text/event-stream";
                
                connection.PrepareRequest(_request);

            }, true).ContinueWith(task =>
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

                            Reconnect(connection, data, disconnectToken);
                        }
                    }
                    requestDisposer.Dispose();
                }
                else
                {
                    var response = task.Result;
                    Stream stream = response.GetStream();

                    var eventSource = new EventSourceStreamReader(connection, stream);

                    bool stop = false;

                    var esCancellationRegistration = disconnectToken.SafeRegister(state =>
                    {
                        stop = true;

                        ((IRequest)state).Abort();
                    },
                    _request);

                    eventSource.Opened = () =>
                    {
                        // If we're not reconnecting, then we're starting the transport for the first time. Trigger callback only on first start.
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
                            TransportHelper.ProcessResponse(connection, sseEvent.Data, out timedOut, out disconnected);

                            if (disconnected)
                            {
                                stop = true;
                                connection.Disconnect();
                            }
                        }
                    };

                    eventSource.Closed = exception =>
                    {
                        if (exception != null)
                        {
                            // Check if the request is aborted
                            bool isRequestAborted = ExceptionHelper.IsRequestAborted(exception);

                            if (!isRequestAborted)
                            {
                                // Don't raise exceptions if the request was aborted (connection was stopped).
                                connection.OnError(exception);
                            }
                        }
                        requestDisposer.Dispose();
                        esCancellationRegistration.Dispose();
                        response.Dispose();

                        if (stop)
                        {
                            CompleteAbort();
                        }
                        else if (TryCompleteAbort())
                        {
                            // Abort() was called, so don't reconnect
                        }
                        else
                        {
                            Reconnect(connection, data, disconnectToken);
                        }
                    };

                    eventSource.Start();
                }
            });

            var requestCancellationRegistration = disconnectToken.SafeRegister(state =>
            {
                if (state != null)
                {
                    // This will no-op if the request is already finished.
                    ((IRequest)state).Abort();
                }

                if (errorCallback != null)
                {
                    callbackInvoker.Invoke((cb, token) =>
                    {
#if NET35 || WINDOWS_PHONE
                        cb(new OperationCanceledException(Resources.Error_ConnectionCancelled));
#else
                        cb(new OperationCanceledException(Resources.Error_ConnectionCancelled, token));
#endif
                    }, errorCallback, disconnectToken);
                }
            }, _request);

            requestDisposer.Set(requestCancellationRegistration);

            if (errorCallback != null)
            {
                TaskAsyncHelper.Delay(ConnectionTimeout).Then(() =>
                {
                    callbackInvoker.Invoke((conn, cb) =>
                    {
                        // Abort the request before cancelling
                        _request.Abort();

                        // Connection timeout occurred
                        cb(new TimeoutException());
                    },
                    connection,
                    errorCallback);
                });
            }
        }

        public override void LostConnection(IConnection connection)
        {
            if (_request != null)
            {
                _request.Abort();
            }
        }
    }
}
