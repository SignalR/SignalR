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

        protected override void OnStart(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            OpenConnection(connection, connectionData, disconnectToken, reconnecting: false);
        }

        protected override void OnStartFailed()
        {
            // if the transport failed to start we want to stop it silently.
            _stop = true;

            _request.Abort();
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
                    OpenConnection(connection, data, disconnectToken, reconnecting: true);
                }
            });
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "We will refactor later.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "We will refactor later.")]
        internal void OpenConnection(IConnection connection, string data, CancellationToken disconnectToken, bool reconnecting)
        {
            // If we're reconnecting add /connect to the url
            var url = reconnecting
                ? UrlBuilder.BuildReconnect(connection, Name, data)
                : UrlBuilder.BuildConnect(connection, Name, data);

            connection.Trace(TraceLevels.Events, "SSE: GET {0}", url);

            var getTask = HttpClient.Get(url, req =>
            {
                _request = req;
                _request.Accept = "text/event-stream";

                connection.PrepareRequest(_request);

            }, isLongRunning: true);

            var requestCancellationRegistration = disconnectToken.SafeRegister(state =>
            {
                _stop = true;

                // This will no-op if the request is already finished.
                ((IRequest)state).Abort();
            }, _request);
            
            getTask.ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    var exception = task.IsCanceled
                        ? new OperationCanceledException(Resources.Error_TaskCancelledException)
                        : task.Exception.Unwrap(); 

                    if (!reconnecting)
                    {
                        TransportFailed(exception);
                    }
                    else if (!_stop)
                    {
                        // Only raise the error event if we failed to reconnect
                        connection.OnError(exception);

                        Reconnect(connection, data, disconnectToken);
                    }

                    requestCancellationRegistration.Dispose();
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
                        if (sseEvent.EventType == EventType.Data &&
                            !sseEvent.Data.Equals("initialized", StringComparison.OrdinalIgnoreCase))
                        {
                            ProcessResponse(connection, sseEvent.Data);
                        }
                    };

                    eventSource.Closed = exception =>
                    {
                        if (exception != null)
                        {
                            // Check if the request is aborted
                            if (!ExceptionHelper.IsRequestAborted(exception))
                            {
                                // Don't raise exceptions if the request was aborted (connection was stopped).
                                connection.OnError(exception);
                            }
                        }

                        requestCancellationRegistration.Dispose();
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
