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
        private NegotiationResponse _negotiationResponse;
        private IConnection _connection;
        private string _connectionData;

        private IRequest _currentRequest;
        ThreadSafeInvoker _reconnectInvoker;
        private int _running;
        private readonly object _stopLock = new object();

        private Action<IRequest> _onAbort;
        private Action<Exception> _onError;
        private Action _initReceived;

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
                // Don't check for keep alives if the server didn't send back the "LongPollDelay" as
                // part of the response to /negotiate. That indicates the server is running an older
                // version of SignalR that doesn't send long polling keep alives.
                return _negotiationResponse != null &&
                       _negotiationResponse.LongPollDelay.HasValue;
            }
        }

        public override Task<NegotiationResponse> Negotiate(IConnection connection, string connectionData)
        {
            return
                base.Negotiate(connection, connectionData)
                    .Then(negotiationResponse => _negotiationResponse = negotiationResponse);
        }

        protected override void OnStart(IConnection connection,
                                        string connectionData,
                                        CancellationToken disconnectToken,
                                        TransportInitializationHandler initializeHandler)
        {
            _connection = connection;
            _connectionData = connectionData;

            // a new reconnectInvoker is created on each poll
            _reconnectInvoker = new ThreadSafeInvoker();

            var negotiateInitializer = new NegotiateInitializer(initializeHandler);

            Action<IRequest> initializeAbort = request => { negotiateInitializer.Abort(disconnectToken); };

            _initReceived = initializeHandler.InitReceived;
            _onError = negotiateInitializer.Complete;
            _onAbort = initializeAbort;

            // If the transport fails to initialize we want to silently stop
            initializeHandler.OnFailure += StopPolling;

            var disconnectRegistration = disconnectToken.SafeRegister(state =>
            {
                _reconnectInvoker.Invoke();
                StopPolling();
            }, null);

            // Once we've initialized the connection we need to tear down the initializer functions and assign the appropriate onMessage function
            negotiateInitializer.Initialized += () =>
            {
                _onError = exception =>
                {
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
                };

                _onAbort = _ =>
                {
                    disconnectRegistration.Dispose();

                    // Complete any ongoing calls to Abort()
                    // If someone calls Abort() later, have it no-op
                    AbortHandler.CompleteAbort();
                }; ;
            };

            StartPolling();
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
            if (connection.EnsureReconnecting())
            {
                LostConnection();
            }
        }


        public void StartPolling()
        {
            if (Interlocked.Exchange(ref _running, 1) == 0)
            {
                Poll();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flowed back to user.")]
        private void Poll()
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
                string url = ResolveUrl();

                HttpClient.Post(url, request =>
                {
                    _connection.PrepareRequest(request);
                    _currentRequest = request;

                    // This is called just prior to posting the request to ensure that any in-flight polling request
                    // is always executed before an OnAfterPoll
                    TryDelayedReconnect(_connection, _reconnectInvoker);
                }, isLongRunning: true)
                .ContinueWith(task =>
                {
                    var next = TaskAsyncHelper.Empty;
                    Exception exception = null;

                    if (task.IsFaulted || task.IsCanceled)
                    {
                        if (task.IsCanceled)
                        {
                            exception = new OperationCanceledException(Resources.Error_TaskCancelledException);
                        }
                        else
                        {
                            exception = task.Exception.Unwrap();
                        }

                        _onError(exception);
                    }
                    else
                    {
                        try
                        {
                            next = task.Result.ReadAsString(OnChunk).Then(raw => OnMessage(raw));
                        }
                        catch (Exception ex)
                        {
                            exception = ex;

                            _onError(exception);
                        }
                    }

                    next.Finally(
                        state => OnAfterPoll((Exception) state).Then(() => Poll()), 
                        exception);
                });
            }
        }

        private string ResolveUrl()
        {
            string url;

            if (_connection.MessageId == null)
            {
                url = UrlBuilder.BuildConnect(_connection, Name, _connectionData);
                _connection.Trace(TraceLevels.Events, "LP Connect: {0}", url);
            }
            else if (IsReconnecting(_connection))
            {
                url = UrlBuilder.BuildReconnect(_connection, Name, _connectionData);
                _connection.Trace(TraceLevels.Events, "LP Reconnect: {0}", url);
            }
            else
            {
                url = UrlBuilder.BuildPoll(_connection, Name, _connectionData);
                _connection.Trace(TraceLevels.Events, "LP Poll: {0}", url);
            }

            return url;
        }

        private void OnMessage(string message)
        {
            _connection.Trace(TraceLevels.Messages, "LP: OnMessage({0})", message);

            var shouldReconnect = ProcessResponse(_connection, message, _initReceived);

            if (IsReconnecting(_connection))
            {
                // If the timeout for the reconnect hasn't fired as yet just fire the 
                // event here before any incoming messages are processed
                TryReconnect(_connection, _reconnectInvoker);
            }

            if (shouldReconnect)
            {
                // Transition into reconnecting state
                _connection.EnsureReconnecting();
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

        /// <summary>
        /// Aborts the currently active polling request thereby forcing a reconnect.
        /// This will not trigger OnAbort.
        /// </summary>
        public void LostConnection()
        {
            lock (_stopLock)
            {
                if (_currentRequest != null)
                {
                    _currentRequest.Abort();
                }
            }
        }

        /// <summary>
        /// Fully stops the Polling Request Handlers.
        /// </summary>
        private void StopPolling()
        {
            lock (_stopLock)
            {
                if (Interlocked.Exchange(ref _running, 0) == 1)
                {
                    Abort();
                }
            }
        }

        /// <summary>
        /// Aborts the currently active polling request, does not stop the Polling Request Handler.
        /// </summary>
        private void Abort()
        {
            _onAbort(_currentRequest);

            if (_currentRequest != null)
            {
                // This will no-op if the request is already finished
                _currentRequest.Abort();
            }
        }

        private static bool IsKeepAlive(ArraySegment<byte> readBuffer)
        {
            return readBuffer.Count == 1
                && readBuffer.Array[readBuffer.Offset] == (byte)' ';
        }

        private bool OnChunk(ArraySegment<byte> readBuffer)
        {
            if (IsKeepAlive(readBuffer))
            {
                _connection.MarkLastMessage();
                return false;
            }

            return true;
        }

    }
}
