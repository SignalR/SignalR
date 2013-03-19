using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    internal class PollingRequestHandler
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
        }
        
        /// <summary>
        /// Used to generate the Url that is posted to for the poll.
        /// </summary>
        public Func<string> ResolveUrl { get; set; }

        /// <summary>
        /// Allows modification of the IRequest parameter before using it in a poll.
        /// </summary>
        public Action<IRequest> PrepareRequest { get; set; }
        
        /// <summary>
        /// Sends the string based message to the callback.
        /// </summary>
        public Action<string> OnMessage { get; set; }

        /// <summary>
        /// If the poll errors OnError gets triggered and passes the exception.
        /// </summary>
        public Action<Exception> OnError { get; set; }

        /// <summary>
        /// Triggers when the polling request is in flight
        /// </summary>
        public Action OnPolling { get; set; }

        /// <summary>
        /// Triggers before a new polling request is attempted.  
        /// Passes in an exception if the Poll errored, null otherwise.
        /// Expects the return as a task in order to allow modification of timing for subsequent polls.
        /// </summary>
        public Func<Exception, Task> OnAfterPoll { get; set; }

        /// <summary>
        /// Fired when the current poll request was aborted, passing in the soon to be aborted request.
        /// </summary>
        public Action<IRequest> OnAbort { get; set; }

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
                })
                .ContinueWith(task =>
                {
                    var next = TaskAsyncHelper.Empty;
                    Exception exception = null;

                    if (task.IsFaulted)
                    {
                        exception = task.Exception.Unwrap();

                        OnError(exception);
                    }
                    else
                    {
                        try
                        {
                            next = task.Result.ReadAsString().Then(raw => OnMessage(raw));
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

            // This is called at the bottom since the above code is run async
            // Represents when the PollingRequestHandler has a poll in flight
            OnPolling();
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
        public void Abort()
        {
            if (OnAbort != null)
            {
                OnAbort(_currentRequest);
            }

            if (_currentRequest != null)
            {
                // This will no-op if the request is already finished
                _currentRequest.Abort();
            }
        }
    }
}
