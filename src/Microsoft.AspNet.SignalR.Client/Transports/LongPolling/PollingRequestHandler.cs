// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class PollingRequestHandler
    {
        private IHttpClient _httpClient;
        private IRequest _currentRequest;
        private int _running;
        private object _stopLock;

        public PollingRequestHandler(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _running = 0;
            _stopLock = new object();

            // Set default events
            ResolveUrl = () => "";
            PrepareRequest = _ => { };
            OnMessage = _ => { };
            OnError = _ => { };
            OnPolling = () => { };
            OnAfterPoll = _ => TaskAsyncHelper.Empty;
            OnAbort = _ => { };
            OnKeepAlive = () => { };
        }

        /// <summary>
        /// Used to generate the Url that is posted to for the poll.
        /// </summary>
        public Func<string> ResolveUrl { get; set; }

        /// <summary>
        /// Allows modification of the IRequest parameter before using it in a poll.
        /// </summary>
        public event Action<IRequest> PrepareRequest;

        /// <summary>
        /// Sends the string based message to the callback.
        /// </summary>
        public event Action<string> OnMessage;

        /// <summary>
        /// If the poll errors OnError gets triggered and passes the exception.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Triggers when the polling request is in flight
        /// </summary>
        public event Action OnPolling;

        /// <summary>
        /// Triggers before a new polling request is attempted.  
        /// Passes in an exception if the Poll errored, null otherwise.
        /// Expects the return as a task in order to allow modification of timing for subsequent polls.
        /// </summary>
        public Func<Exception, Task> OnAfterPoll { get; set; }

        /// <summary>
        /// Fired when the current poll request was aborted, passing in the soon to be aborted request.
        /// </summary>
        public event Action<IRequest> OnAbort;

        /// <summary>
        /// Fired when the current poll request receives a keep alive.
        /// </summary>
        public event Action OnKeepAlive;

        /// <summary>
        /// Starts the Polling Request Handler.
        /// </summary>
        public void Start()
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

                _httpClient.Post(url, request =>
                {
                    PrepareRequest(request);
                    _currentRequest = request;

                    // This is called just prior to posting the request to ensure that any in-flight polling request
                    // is always executed before an OnAfterPoll
                    OnPolling();
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

                        OnError(exception);
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

                            OnError(exception);
                        }
                    }

                    next.Finally(state =>
                    {
                        OnAfterPoll((Exception)state).Then(() => Poll());
                    },
                    exception);
                });
            }            
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
        public void Stop()
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
            OnAbort(_currentRequest);

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
                OnKeepAlive();
                return false;
            }

            return true;
        }
    }
}
