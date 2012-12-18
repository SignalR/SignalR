// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "This will never be disposed."), DebuggerStepThrough]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in future.")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
        protected void Complete(bool didCompleteSynchronously)
        {
            if (this.isCompleted)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Error_MultipleOperationException,
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
                        throw new CallbackException(String.Format(CultureInfo.CurrentCulture, Resources.Error_AsyncCallbackThrewException), e);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
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

        protected AsyncCallback PrepareAsyncCompletion(AsyncCompletion completionCallback)
        {
            this.nextAsyncCompletion = completionCallback;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        protected bool SyncContinue(IAsyncResult result)
        {
            AsyncCompletion continueCallback;
            if (TryContinueHelper(result, out continueCallback))
            {
                return continueCallback(result);
            }
            else
            {
                return false;
            }
        }

        bool TryContinueHelper(IAsyncResult result, out AsyncCompletion continueCallback)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            continueCallback = null;

            if (!result.CompletedSynchronously)
            {
                return false;
            }

            continueCallback = GetNextCompletion();
            if (continueCallback == null)
            {
                ThrowInvalidAsyncResult(string.Format(CultureInfo.CurrentCulture, Resources.Error_OnlyOnceChecOrSyncPerOperation));
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
                    Resources.Error_IncorrectImplOfIAsyncResultReturningBadValues,
                    result.GetType()));
        }

        protected static void ThrowInvalidAsyncResult(string debugText)
        {
            string message = string.Format(CultureInfo.CurrentCulture, Resources.Error_IncorrectImplOfIAsyncResult, debugText);
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
                    string.Format(CultureInfo.CurrentCulture, Resources.Error_IncorrectIAsyncResultProvidedToEndMethod),
                    "result");
            }

            if (thisPtr.endCalled)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_EndCannotBeCalledTwiceOnSameAsyncResult));
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
