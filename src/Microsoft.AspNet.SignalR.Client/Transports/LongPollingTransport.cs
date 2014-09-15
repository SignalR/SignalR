// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class LongPollingTransport : HttpBasedTransport
    {
        private IRequest _currentRequest;
        private int _running;
        private readonly object _stopLock = new object();
        private ThreadSafeInvoker _reconnectInvoker;
        private IDisposable _disconnectRegistration;

        /// <summary>
        /// The time to wait after a connection drops to try reconnecting.
        /// </summary>
        public TimeSpan ReconnectDelay { get; set; }

        /// <summary>
        /// The time to wait after an error happens to continue polling.
        /// </summary>
        public TimeSpan ErrorDelay { get; set; }

        public LongPollingTransport()
            : this(new DefaultHttpClient())
        {
        }

        public LongPollingTransport(IHttpClient httpClient)
            : base(httpClient, "longPolling")
        {
            ReconnectDelay = TimeSpan.FromSeconds(5);
            ErrorDelay = TimeSpan.FromSeconds(2);
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

        protected override void OnStart(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            _disconnectRegistration = disconnectToken.SafeRegister(state =>
            {
                // _reconnectInvoker can be null if disconnectToken is tripped before the polling loop is started
                if (_reconnectInvoker != null)
                {
                    _reconnectInvoker.Invoke();
                }

                StopPolling();
            }, null);

            StartPolling(connection, connectionData);
        }

        protected override void OnStartFailed()
        {
            // If the transport fails to initialize we want to silently stop
            StopPolling();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to user.")]
        private void Poll(IConnection connection, string connectionData)
        {
            // This is to ensure that we do not accidently fire off another poll after being told to stop
            lock (_stopLock)
            {
                // Only poll if we're running
                if (_running == 0)
                {
                    return;
                }

                // A url is required
                var url = ResolveUrl(connection, connectionData);

                HttpClient.Post(url, request =>
                {
                    connection.PrepareRequest(request);
                    _currentRequest = request;

                    // This is called just prior to posting the request to ensure that any in-flight polling request
                    // is always executed before an OnAfterPoll
                    TryDelayedReconnect(connection, _reconnectInvoker);
                }, isLongRunning: true)
                .ContinueWith(task =>
                {
                    var next = TaskAsyncHelper.Empty;
                    Exception exception = null;

                    if (task.IsFaulted || task.IsCanceled)
                    {
                        exception = task.IsCanceled
                            ? new OperationCanceledException(Resources.Error_TaskCancelledException)
                            : task.Exception.Unwrap();

                        OnError(connection, exception);
                    }
                    else
                    {
                        try
                        {
                            next = task.Result.ReadAsString(readBuffer => OnChunk(connection, readBuffer))
                                .Then(raw => OnMessage(connection, raw));
                        }
                        catch (Exception ex)
                        {
                            exception = ex;

                            OnError(connection, exception);
                        }
                    }

                    next.Finally(
                        state => OnAfterPoll((Exception) state).Then(() => Poll(connection, connectionData)),
                        exception);
                });
            }
        }

        /// <summary>
        /// Starts the polling loop.
        /// </summary>
        internal void StartPolling(IConnection connection, string connectionData)
        {
            if (Interlocked.Exchange(ref _running, 1) == 0)
            {
                // reconnectInvoker is created new on each poll
                _reconnectInvoker = new ThreadSafeInvoker();

                Poll(connection, connectionData);
            }
        }

        /// <summary>
        /// Fully stops the polling loop.
        /// </summary>
        private void StopPolling()
        {
            lock (_stopLock)
            {
                if (Interlocked.Exchange(ref _running, 0) == 1)
                {
                    _disconnectRegistration.Dispose();

                    // Complete any ongoing calls to Abort()
                    // If someone calls Abort() later, have it no-op
                    AbortHandler.CompleteAbort();

                    if (_currentRequest != null)
                    {
                        // This will no-op if the request is already finished
                        _currentRequest.Abort();
                    }
                }
            }
        }

        private string ResolveUrl(IConnection connection, string connectionData)
        {
            string url;

            if (connection.MessageId == null)
            {
                url = UrlBuilder.BuildConnect(connection, Name, connectionData);
                connection.Trace(TraceLevels.Events, "LP Connect: {0}", url);
            }
            else if (IsReconnecting(connection))
            {
                url = UrlBuilder.BuildReconnect(connection, Name, connectionData);
                connection.Trace(TraceLevels.Events, "LP Reconnect: {0}", url);
            }
            else
            {
                url = UrlBuilder.BuildPoll(connection, Name, connectionData);
                connection.Trace(TraceLevels.Events, "LP Poll: {0}", url);
            }

            return url;
        }

        private void OnMessage(IConnection connection, string message)
        {
            connection.Trace(TraceLevels.Messages, "LP: OnMessage({0})", message);

            var shouldReconnect = ProcessResponse(connection, message);

            if (IsReconnecting(connection))
            {
                // If the timeout for the reconnect hasn't fired as yet just fire the 
                // event here before any incoming messages are processed
                TryReconnect(connection, _reconnectInvoker);
            }

            if (shouldReconnect)
            {
                // Transition into reconnecting state
                connection.EnsureReconnecting();
            }
        }

        private Task OnAfterPoll(Exception exception)
        {
            if (AbortHandler.TryCompleteAbort())
            {
                // Abort() was called, so don't reconnect
                StopPolling();
            }
            else
            {
                _reconnectInvoker = new ThreadSafeInvoker();

                if (exception != null)
                {
                    // Delay polling by the error delay
                    return TaskAsyncHelper.Delay(ErrorDelay);
                }
            }

            return TaskAsyncHelper.Empty;
        }

        // internal virtual for testing
        internal virtual void OnError(IConnection connection, Exception exception)
        {
            TransportFailed(exception);
            _reconnectInvoker.Invoke();

            if (!TransportHelper.VerifyLastActive(connection))
            {
                StopPolling();
            }

            // Transition into reconnecting state
            connection.EnsureReconnecting();

            // Sometimes a connection might have been closed by the server before we get to write anything
            // so just try again and raise OnError.
            if (!ExceptionHelper.IsRequestAborted(exception) && !(exception is IOException))
            {
                connection.OnError(exception);
            }
        }

        private static bool OnChunk(IConnection connection, ArraySegment<byte> readBuffer)
        {
            if (IsKeepAlive(readBuffer))
            {
                connection.MarkLastMessage();
                return false;
            }

            return true;
        }

        private static bool IsKeepAlive(ArraySegment<byte> readBuffer)
        {
            return readBuffer.Count == 1
                && readBuffer.Array[readBuffer.Offset] == (byte)' ';
        }

        /// <summary>
        /// Aborts the currently active polling request thereby forcing a reconnect.
        /// </summary>
        public override void LostConnection(IConnection connection)
        {
            if (connection.EnsureReconnecting())
            {
                lock (_stopLock)
                {
                    if (_currentRequest != null)
                    {
                        _currentRequest.Abort();
                    }
                }
            }
        }

        private void TryDelayedReconnect(IConnection connection, ThreadSafeInvoker reconnectInvoker)
        {
            if (IsReconnecting(connection))
            {
                // Fire the reconnect event after the delay.
                TaskAsyncHelper.Delay(ReconnectDelay)
                    .Then(() => TryReconnect(connection, reconnectInvoker));
            }
        }

        private static void TryReconnect(IConnection connection, ThreadSafeInvoker reconnectInvoker)
        {
            reconnectInvoker.Invoke(FireReconnected, connection);
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
    }
}
