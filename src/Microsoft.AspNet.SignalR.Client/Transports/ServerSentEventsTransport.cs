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
        private bool _stop;

        public ServerSentEventsTransport()
            : this(new DefaultHttpClient())
        {
        }

        public ServerSentEventsTransport(IHttpClient httpClient)
            : base(httpClient, "serverSentEvents")
        {
            ReconnectDelay = TimeSpan.FromSeconds(2);
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
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        protected override void OnStart(IConnection connection,
                                        string connectionData,
                                        CancellationToken disconnectToken,
                                        TransportInitializationHandler initializeHandler)
        {
            if (initializeHandler == null)
            {
                throw new ArgumentNullException("initializeHandler");
            }

            // Tie into the OnFailure event so that we can stop the transport silently.
            initializeHandler.OnFailure += () =>
            {
                _stop = true;

                _request.Abort();
            };

            OpenConnection(connection, connectionData, disconnectToken, initializeHandler.Success, initializeHandler.Fail);
        }

        private void Reconnect(IConnection connection, string data, CancellationToken disconnectToken)
        {
            // Need to verify before the task delay occurs because an application sleep could occur during the delayed duration.
            if (!TransportHelper.VerifyLastActive(connection))
            {
                return;
            }

            // Wait for a bit before reconnecting
            TaskAsyncHelper.Delay(ReconnectDelay).Then(() =>
            {
                if (!TransportHelper.VerifyLastActive(connection))
                {
                    return;
                }

                // FIX: Race if Connection is stopped and completely restarted between checking the token and calling
                //      connection.EnsureReconnecting()
                if (!disconnectToken.IsCancellationRequested && connection.EnsureReconnecting())
                {
                    // Now attempt a reconnect
                    OpenConnection(connection, data, disconnectToken, initializeCallback: null, errorCallback: null);
                }
            });
        }

        public void OpenConnection(IConnection connection, Action<Exception> errorCallback)
        {
            OpenConnection(connection, null, CancellationToken.None, () => { }, errorCallback);
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
            Action initializeInvoke = () =>
            {
                callbackInvoker.Invoke(initializeCallback);
            };
            var url = connection.Url + (reconnecting ? "reconnect" : "connect") + GetReceiveQueryString(connection, data);

            connection.Trace(TraceLevels.Events, "SSE: GET {0}", url);

            HttpClient.Get(url, req =>
            {
                _request = req;
                _request.Accept = "text/event-stream";

                connection.PrepareRequest(_request);

            }, isLongRunning: true).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Exception exception;

                    if (task.IsCanceled)
                    {
                        exception = new OperationCanceledException(Resources.Error_TaskCancelledException);
                    }
                    else
                    {
                        exception = task.Exception.Unwrap();
                    }

                    if (errorCallback != null)
                    {
                        callbackInvoker.Invoke((cb, ex) => cb(ex), errorCallback, exception);
                    }
                    else if (!_stop && reconnecting)
                    {
                        // Only raise the error event if we failed to reconnect
                        connection.OnError(exception);

                        Reconnect(connection, data, disconnectToken);
                    }
                    requestDisposer.Dispose();
                }
                else
                {
                    // If the disconnect token is canceled the response to the task doesn't matter.
                    if (disconnectToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var response = task.Result;
                    Stream stream = response.GetStream();

                    var eventSource = new EventSourceStreamReader(connection, stream);

                    var esCancellationRegistration = disconnectToken.SafeRegister(state =>
                    {
                        _stop = true;

                        ((IRequest)state).Abort();
                    },
                    _request);

                    eventSource.Opened = () =>
                    {
                        // This will noop if we're not in the reconnecting state
                        if (connection.ChangeState(ConnectionState.Reconnecting, ConnectionState.Connected))
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

                            bool shouldReconnect;
                            bool disconnected;
                            TransportHelper.ProcessResponse(connection, sseEvent.Data, out shouldReconnect, out disconnected, initializeInvoke);

                            if (disconnected)
                            {
                                _stop = true;
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

                        if (_stop)
                        {
                            AbortHandler.CompleteAbort();
                        }
                        else if (AbortHandler.TryCompleteAbort())
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
                        cb(new OperationCanceledException(Resources.Error_ConnectionCancelled, token));
                    }, errorCallback, disconnectToken);
                }
            }, _request);

            requestDisposer.Set(requestCancellationRegistration);
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
