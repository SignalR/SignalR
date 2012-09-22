namespace SignalR.ServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;

    [DebuggerStepThrough]
    abstract class AsyncResult : IAsyncResult
    {
        static AsyncCallback asyncCompletionWrapperCallback;
        AsyncCallback callback;
        bool completedSynchronously;
        bool endCalled;
        Exception exception;
        bool isCompleted;
        AsyncCompletion nextAsyncCompletion;
        object state;

        ManualResetEvent manualResetEvent;

        object thisLock;

        protected AsyncResult(AsyncCallback callback, object state)
        {
            this.callback = callback;
            this.state = state;
            this.thisLock = new object();
        }

        public object AsyncState
        {
            get
            {
                return this.state;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (this.manualResetEvent != null)
                {
                    return this.manualResetEvent;
                }

                lock (this.ThisLock)
                {
                    if (this.manualResetEvent == null)
                    {
                        this.manualResetEvent = new ManualResetEvent(isCompleted);
                    }
                }

                return this.manualResetEvent;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return this.completedSynchronously;
            }
        }

        public bool HasCallback
        {
            get
            {
                return this.callback != null;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return this.isCompleted;
            }
        }

        // used in conjunction with PrepareAsyncCompletion to allow for finally blocks
        protected Action<AsyncResult, Exception> OnCompleting { get; set; }

        protected object ThisLock
        {
            get
            {
                return this.thisLock;
            }
        }

        // subclasses like TraceAsyncResult can use this to wrap the callback functionality in a scope
        protected Action<AsyncCallback, IAsyncResult> VirtualCallback
        {
            get;
            set;
        }

        protected void Complete(bool didCompleteSynchronously)
        {
            if (this.isCompleted)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "The IAsyncResult implementation '{0}' tried to complete a single operation multiple times. " + 
                        "This could be caused by an incorrect application IAsyncResult implementation or " + 
                        "other extensibility code, such as an IAsyncResult that returns incorrect CompletedSynchronously " + 
                        "values or invokes the AsyncCallback multiple times.",
                        this.GetType()));
            }

            this.completedSynchronously = didCompleteSynchronously;
            if (this.OnCompleting != null)
            {
                try
                {
                    this.OnCompleting(this, this.exception);
                }
                catch (Exception e)
                {
                    this.exception = e;
                }
            }

            if (didCompleteSynchronously)
            {
                Debug.Assert(this.manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
                this.isCompleted = true;
            }
            else
            {
                lock (this.ThisLock)
                {
                    this.isCompleted = true;
                    if (this.manualResetEvent != null)
                    {
                        this.manualResetEvent.Set();
                    }
                }
            }

            if (this.callback != null)
            {
                try
                {
                    if (this.VirtualCallback != null)
                    {
                        this.VirtualCallback(this.callback, this);
                    }
                    else
                    {
                        this.callback(this);
                    }
                }
                catch (Exception e)
                {
                    // Throw in a different thread so that it becomes unhandled exception.
                    Task task = Task.Factory.StartNew(() =>
                    {
                        throw new CallbackException("An AsyncCallback threw an exception.", e);
                    });

                    task.Wait();
                }
            }
        }

        protected void Complete(bool didCompleteSynchronously, Exception e)
        {
            this.exception = e;
            Complete(didCompleteSynchronously);
        }

        static void AsyncCompletionWrapperCallback(IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            if (result.CompletedSynchronously)
            {
                return;
            }

            AsyncResult thisPtr = (AsyncResult)result.AsyncState;

            AsyncCompletion callback = thisPtr.GetNextCompletion();
            if (callback == null)
            {
                ThrowInvalidAsyncResult(result);
            }

            bool completeSelf;
            Exception completionException = null;
            try
            {
                completeSelf = callback(result);
            }
            catch (Exception e)
            {
                completeSelf = true;
                completionException = e;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completionException);
            }
        }

        protected AsyncCallback PrepareAsyncCompletion(AsyncCompletion callback)
        {
            this.nextAsyncCompletion = callback;
            if (AsyncResult.asyncCompletionWrapperCallback == null)
            {
                AsyncResult.asyncCompletionWrapperCallback = new AsyncCallback(AsyncCompletionWrapperCallback);
            }
            return AsyncResult.asyncCompletionWrapperCallback;
        }

        protected bool CheckSyncContinue(IAsyncResult result)
        {
            AsyncCompletion dummy;
            return TryContinueHelper(result, out dummy);
        }

        protected bool SyncContinue(IAsyncResult result)
        {
            AsyncCompletion callback;
            if (TryContinueHelper(result, out callback))
            {
                return callback(result);
            }
            else
            {
                return false;
            }
        }

        bool TryContinueHelper(IAsyncResult result, out AsyncCompletion callback)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            callback = null;

            if (!result.CompletedSynchronously)
            {
                return false;
            }

            callback = GetNextCompletion();
            if (callback == null)
            {
                ThrowInvalidAsyncResult("Only call Check/SyncContinue once per async operation (once per PrepareAsyncCompletion).");
            }
            return true;
        }

        AsyncCompletion GetNextCompletion()
        {
            AsyncCompletion result = this.nextAsyncCompletion;
            this.nextAsyncCompletion = null;
            return result;
        }

        protected static void ThrowInvalidAsyncResult(IAsyncResult result)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "An incorrect implementation of the IAsyncResult interface ({0}) may be returning incorrect values " +
                    "from the CompletedSynchronously property or calling the AsyncCallback more than once.",
                    result.GetType()));
        }

        protected static void ThrowInvalidAsyncResult(string debugText)
        {
            string message = string.Format(CultureInfo.CurrentCulture, "An incorrect implementation of the IAsyncResult interface. {0}", debugText);
            throw new InvalidOperationException(message);
        }

        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : AsyncResult
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            TAsyncResult thisPtr = result as TAsyncResult;

            if (thisPtr == null)
            {
                throw new ArgumentException(
                    "An incorrect IAsyncResult was provided to an 'End' method. The IAsyncResult object passed to 'End' must be the one returned from the matching 'Begin' or passed to the callback provided to 'Begin'.", 
                    "result");
            }

            if (thisPtr.endCalled)
            {
                throw new InvalidOperationException("End cannot be called twice on the same AsyncResult.");
            }

            thisPtr.endCalled = true;

            if (!thisPtr.isCompleted)
            {
                thisPtr.AsyncWaitHandle.WaitOne();
            }

            if (thisPtr.manualResetEvent != null)
            {
                thisPtr.manualResetEvent.Close();
            }

            thisPtr.callback = null;

            if (thisPtr.exception != null)
            {
                //ExceptionDispatchInfo exceptionDispatchInfo = ExceptionDispatchInfo.Capture(thisPtr.exception);
                //exceptionDispatchInfo.Throw();

                throw thisPtr.exception;
            }

            return thisPtr;
        }

        protected delegate bool AsyncCompletion(IAsyncResult result);
    }

    abstract class AsyncResult<TAsyncResult> : AsyncResult
        where TAsyncResult : AsyncResult<TAsyncResult>
    {
        protected AsyncResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
        }

        public static TAsyncResult End(IAsyncResult asyncResult)
        {
            return AsyncResult.End<TAsyncResult>(asyncResult);
        }
    }
}
