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

        protected override void OnStart(IConnection connection,
                                        string data,
                                        CancellationToken disconnectToken,
                                        Action end,
                                        Action initializeCallback,
                                        Action<Exception> errorCallback)
        {
            var disconnectInvoker = new ThreadSafeInvoker();
            PollingLoop(connection, data, disconnectToken, () => disconnectInvoker.Invoke(end), initializeCallback, errorCallback);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "We will refactor later.")]
        private void PollingLoop(IConnection connection,
                                 string data,
                                 CancellationToken disconnectToken,
                                 Action end,
                                 Action initializeCallback,
                                 Action<Exception> errorCallback,
                                 bool raiseReconnect = false)
        {
            string url = connection.Url;

            IRequest request = null;
            var reconnectInvoker = new ThreadSafeInvoker();
            var callbackInvoker = new ThreadSafeInvoker();

            if (connection.MessageId == null)
            {
                url += "connect";
            }
            else if (raiseReconnect)
            {
                url += "reconnect";

                if (!connection.EnsureReconnecting())
                {
                    return;
                }
            }

            url += GetReceiveQueryString(connection, data);

#if NET35
            Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "LP: {0}", (object)url));
#else
            Debug.WriteLine("LP: {0}", (object)url);
#endif

            HttpClient.PostAsync(url, req => 
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
                        var raw = task.Result.ReadAsString();

#if NET35
                        Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "LP Receive: {0}", (object)raw));
#else
                        Debug.WriteLine("LP Receive: {0}", (object)raw);
#endif

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
                                                end,
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
                                            end,
                                            initializeCallback: null,
                                            errorCallback: null,
                                            raiseReconnect: shouldRaiseReconnect);
                            }
                        }
                    }
                }
            });

            disconnectToken.SafeRegister(req =>
            {
                if (req != null)
                {
                    req.Abort();
                }

                reconnectInvoker.Invoke();
                if (errorCallback != null)
                {
                    callbackInvoker.Invoke((cb, token) =>
                    {
#if NET35
                        cb(new OperationCanceledException(Resources.Error_ConnectionCancelled));
#else
                        cb(new OperationCanceledException(Resources.Error_ConnectionCancelled, token));
#endif
                    }, errorCallback, disconnectToken);
                }

                end();
            }, request);

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
    }
}
