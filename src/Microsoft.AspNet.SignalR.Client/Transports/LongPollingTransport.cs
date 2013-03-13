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
using System.Threading.Tasks;

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

        private void ProcessFaultedPollingResponse(IConnection connection,
                                                   Exception exception,
                                                   string data,
                                                   CancellationToken disconnectToken,
                                                   ThreadSafeInvoker reconnectInvoker,
                                                   ThreadSafeInvoker callbackInvoker,
                                                   Action<Exception> errorCallback)
        {
            bool requestAborted = false;

            reconnectInvoker.Invoke();

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
                                // Raise the reconnect event if we successfully reconnect after failing
                                raiseReconnect: true);
                        }
                    });
                }
            }
        }

        private void ProcessCompletedPollingResponse(IConnection connection,
                                                     string data,
                                                     bool shouldRaiseReconnect,
                                                     CancellationToken disconnectToken)
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

        private bool TryAbortAndDisconnect(IConnection connection, bool disconnectedReceived)
        {
            if (AbortResetEvent != null)
            {
                AbortResetEvent.Set();
                return true;
            }
            else if (disconnectedReceived)
            {
                connection.Disconnect();
                return true;
            }

            return false;
        }

        private void SetupCancellations(IConnection connection,
                                        IRequest request,
                                        bool raiseReconnect,
                                        Action initializeCallback,
                                        CancellationToken disconnectToken,
                                        Disposer requestDisposer,
                                        ThreadSafeInvoker reconnectInvoker,
                                        ThreadSafeInvoker callbackInvoker,
                                        Action<Exception> errorCallback)
        {
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

            // Initial connect
            if (initializeCallback != null)
            {
                TaskAsyncHelper.Delay(ConnectDelay).Then(() =>
                {
                    callbackInvoker.Invoke(initializeCallback);
                });
            }

            // Reconnected
            if (raiseReconnect)
            {
                TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
                {
                    // Fire the reconnect event after the delay.
                    reconnectInvoker.Invoke((conn) => FireReconnected(conn), connection);
                });
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

                        // Chain responses so that we can maintain message order
                        task.Result.ReadAsString().Then(raw =>
                        {

                            connection.Trace.WriteLine("LP: OnMessage({0}, {1})", connection.ConnectionId, raw);

                            TransportHelper.ProcessResponse(connection,
                                                            raw,
                                                            out shouldRaiseReconnect,
                                                            out disconnectedReceived);
                        }).Finally(exception =>
                        {
                            if (!TryAbortAndDisconnect(connection, disconnectedReceived))
                            {
                                ProcessCompletedPollingResponse(connection, data, shouldRaiseReconnect, disconnectToken);
                                requestDisposer.Dispose();
                            }
                        }, null);
                    }
                }
                finally
                {
                    if (task.IsFaulted)
                    {
                        if (!TryAbortAndDisconnect(connection, disconnectedReceived))
                        {
                            ProcessFaultedPollingResponse(connection, task.Exception.Unwrap(), data, disconnectToken, reconnectInvoker, callbackInvoker, errorCallback);
                        }

                        requestDisposer.Dispose();
                    }
                }                
            });

            SetupCancellations(connection, request, raiseReconnect, initializeCallback, disconnectToken, requestDisposer, reconnectInvoker, callbackInvoker, errorCallback);
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
