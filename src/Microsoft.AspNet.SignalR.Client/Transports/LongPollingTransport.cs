// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class LongPollingTransport : HttpBasedTransport
    {
        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        /// <summary>
        /// The time to wait after an error happens to continue polling.
        /// </summary>
        public TimeSpan ErrorDelay { get; set; }

        /// <summary>
        /// The time to wait after the initial connect http request before it is considered
        /// open.
        /// </summary>
        public TimeSpan ConnectDelay { get; set; }


        public LongPollingTransport()
            : this(new DefaultHttpClient())
        {
        }

        public LongPollingTransport(IHttpClient httpClient)
            : base(httpClient, "longPolling")
        {
            ReconnectDelay = TimeSpan.FromSeconds(5);
            ErrorDelay = TimeSpan.FromSeconds(2);
            ConnectDelay = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Indicates whether or not the transport supports keep alive
        /// </summary>
        public override bool SupportsKeepAlive
        {
            get
            {
                return false;
            }
        }

        protected override void OnStart(IConnection connection,
                                        string data,
                                        CancellationToken disconnectToken,
                                        Action initializeCallback,
                                        Action<Exception> errorCallback)
        {
            PollingLoop(connection, data, disconnectToken, initializeCallback, errorCallback);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "We will refactor later.")]
        private void PollingLoop(IConnection connection,
                                 string data,
                                 CancellationToken disconnectToken,
                                 Action initializeCallback,
                                 Action<Exception> errorCallback,
                                 bool raiseReconnect = false)
        {
            string url = connection.Url;

            IRequest request = null;
            var reconnectInvoker = new ThreadSafeInvoker();
            var callbackInvoker = new ThreadSafeInvoker();
            var requestDisposer = new Disposer();

            if (connection.MessageId == null)
            {
                url += "connect";
            }
            else if (raiseReconnect)
            {
                url += "reconnect";

                // FIX: Race if the connection is stopped and completely restarted between checking the token and calling
                //      connection.EnsureReconnecting()
                if (disconnectToken.IsCancellationRequested || !connection.EnsureReconnecting())
                {
                    return;
                }
            }

            url += GetReceiveQueryString(connection, data);

            connection.Trace.WriteLine("LP: {0}", url);

            HttpClient.Post(url, req =>
            {
                request = req;
                connection.PrepareRequest(request);
            }).ContinueWith(task =>
            {
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
                            reconnectInvoker.Invoke((conn) => FireReconnected(conn), connection);
                        }

                        if (initializeCallback != null)
                        {
                            // If the timeout for connect hasn't fired as yet then just fire
                            // the event before any incoming messages are processed
                            callbackInvoker.Invoke(initializeCallback);
                        }

                        // Get the response
                        task.Result.ReadAsString().Then(raw =>
                        {

                            connection.Trace.WriteLine("LP: OnMessage({0}, {1})", connection.ConnectionId, raw);

                            TransportHelper.ProcessResponse(connection,
                                                            raw,
                                                            out shouldRaiseReconnect,
                                                            out disconnectedReceived);
                        });
                    }
                }
                finally
                {
                    if (AbortResetEvent != null)
                    {
                        AbortResetEvent.Set();
                    }
                    else if (disconnectedReceived)
                    {
                        connection.Disconnect();
                    }
                    else
                    {
                        bool requestAborted = false;

                        if (task.IsFaulted)
                        {
                            reconnectInvoker.Invoke();

                            // Raise the reconnect event if we successfully reconnect after failing
                            shouldRaiseReconnect = true;

                            // Get the underlying exception
                            Exception exception = task.Exception.Unwrap();

                            // If the error callback isn't null then raise it and don't continue polling
                            if (errorCallback != null)
                            {
                                callbackInvoker.Invoke((cb, ex) => cb(ex), errorCallback, exception);
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
                                    TaskAsyncHelper.Delay(ErrorDelay).Then(() =>
                                    {
                                        if (!disconnectToken.IsCancellationRequested)
                                        {
                                            PollingLoop(connection,
                                                data,
                                                disconnectToken,
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
                            if (!disconnectToken.IsCancellationRequested)
                            {
                                // Continue polling if there was no error
                                PollingLoop(connection,
                                            data,
                                            disconnectToken,
                                            initializeCallback: null,
                                            errorCallback: null,
                                            raiseReconnect: shouldRaiseReconnect);
                            }
                        }
                    }

                    requestDisposer.Dispose();
                }
            });

            var requestCancellationRegistration = disconnectToken.SafeRegister(state =>
            {
                if (state != null)
                {
                    // This will no-op if the request is already finished.
                    ((IRequest)state).Abort();
                }

                // Prevent the connection state from switching to the reconnected state.
                reconnectInvoker.Invoke();

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
            }, request);

            requestDisposer.Set(requestCancellationRegistration);

            if (initializeCallback != null)
            {
                TaskAsyncHelper.Delay(ConnectDelay).Then(() =>
                {
                    callbackInvoker.Invoke(initializeCallback);
                });
            }

            if (raiseReconnect)
            {
                TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
                {
                    // Fire the reconnect event after the delay. This gives the 
                    reconnectInvoker.Invoke((conn) => FireReconnected(conn), connection);
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static void FireReconnected(IConnection connection)
        {
            // Mark the connection as connected
            if (connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
            {
                connection.OnReconnected();
            }
        }

        public override void LostConnection(IConnection connection)
        {

        }
    }
}
