// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
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
            var requestHandler = new PollingRequestHandler(HttpClient);
            var negotiateInitializer = new NegotiateInitializer(initializeCallback, errorCallback, ConnectDelay);

            // Save the success and abort cases so we can remove them after transport is initialized
            Action<string> initializeSuccess = message => { negotiateInitializer.Complete(); };
            Action<IRequest> initializeAbort = request => { negotiateInitializer.Abort(disconnectToken); };

            requestHandler.OnMessage += initializeSuccess;
            requestHandler.OnError += negotiateInitializer.Complete;
            requestHandler.OnAbort += initializeAbort;

            // Once we've initialized the connection we need to tear down the initializer functions
            negotiateInitializer.Initialized += () =>
            {
                requestHandler.OnMessage -= initializeSuccess;
                requestHandler.OnError -= negotiateInitializer.Complete;
                requestHandler.OnAbort -= initializeAbort;
            };

            // Add additional actions to each of the PollingRequestHandler events
            PollingSetup(connection, data, disconnectToken, requestHandler);

            requestHandler.Start();
            // Start initialization, essentially if we have an assume sucess clause in our negotiateInitializer
            // then we will start the countdown from the point which we start initialization.
            negotiateInitializer.Initialize();
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "We will refactor later.")]
        private void PollingSetup(IConnection connection,
                                  string data,
                                  CancellationToken disconnectToken,
                                  PollingRequestHandler requestHandler)
        {
            // These are created new on each poll
            var reconnectInvoker = new ThreadSafeInvoker();
            var requestDisposer = new Disposer();

            requestHandler.ResolveUrl = () =>
            {
                var url = connection.Url;

                if (connection.MessageId == null)
                {
                    url += "connect";
                }
                else if (IsReconnecting(connection))
                {
                    url += "reconnect";
                }
                else
                {
                    url += "poll";
                }

                url += GetReceiveQueryString(connection, data);

                return url;
            };

            requestHandler.PrepareRequest += req =>
            {
                connection.PrepareRequest(req);
            };

            requestHandler.OnMessage += message =>
            {
                var shouldReconnect = false;
                var disconnectedReceived = false;                

                connection.Trace(TraceLevels.Messages, "LP: OnMessage({0})", message);

                TransportHelper.ProcessResponse(connection,
                                                message,
                                                out shouldReconnect,
                                                out disconnectedReceived);

                if (IsReconnecting(connection))
                {
                    // If the timeout for the reconnect hasn't fired as yet just fire the 
                    // event here before any incoming messages are processed
                    TryReconnect(connection, reconnectInvoker);
                }

                if (shouldReconnect)
                {
                    // Transition into reconnecting state
                    connection.EnsureReconnecting();
                }

                if (disconnectedReceived)
                {
                    connection.Disconnect();
                }
            };

            requestHandler.OnError += exception =>
            {
                reconnectInvoker.Invoke();

                // Transition into reconnecting state
                connection.EnsureReconnecting();

                // Sometimes a connection might have been closed by the server before we get to write anything
                // so just try again and raise OnError.
                if (!ExceptionHelper.IsRequestAborted(exception) && !(exception is IOException))
                {
                    connection.OnError(exception);
                }
                else
                {
                    requestHandler.Stop();
                }
            };

            requestHandler.OnPolling += () =>
            {
                // Capture the cleanup within a closure so it can persist through multiple requests
                TryDelayedReconnect(connection, reconnectInvoker);

                requestDisposer.Set(disconnectToken.SafeRegister(state =>
                {
                    reconnectInvoker.Invoke();
                    requestHandler.Stop();
                }, null));
            };

            requestHandler.OnAfterPoll = exception =>
            {
                requestDisposer.Dispose();

                if (TryCompleteAbort())
                {
                    // Abort() was called, so don't reconnect
                    requestHandler.Stop();
                }
                else
                {
                    requestDisposer = new Disposer();
                    reconnectInvoker = new ThreadSafeInvoker();

                    if (exception != null)
                    {
                        // Delay polling by the error delay
                        return TaskAsyncHelper.Delay(ErrorDelay);
                    }
                }

                return TaskAsyncHelper.Empty;
            };

            requestHandler.OnAbort += _ =>
            {
                // Complete any ongoing calls to Abort()
                // If someone calls Abort() later, have it no-op
                CompleteAbort();
            };
        }

        private void TryDelayedReconnect(IConnection connection, ThreadSafeInvoker reconnectInvoker)
        {
            if (IsReconnecting(connection))
            {
                TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
                {
                    TryReconnect(connection, reconnectInvoker);
                });
            }
        }

        private static void TryReconnect(IConnection connection, ThreadSafeInvoker reconnectInvoker)
        {
            // Fire the reconnect event after the delay.
            reconnectInvoker.Invoke((conn) => FireReconnected(conn), connection);
        }

        private static void FireReconnected(IConnection connection)
        {
            // Mark the connection as connected
            if (connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
            {
                connection.OnReconnected();
            }
        }

        private static bool IsReconnecting(IConnection connection)
        {
            return connection.State == ConnectionState.Reconnecting;
        }

        public override void LostConnection(IConnection connection)
        {

        }
    }
}
