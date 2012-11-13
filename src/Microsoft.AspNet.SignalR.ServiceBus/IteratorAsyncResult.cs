// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    [DebuggerStepThrough]
    abstract class IteratorAsyncResult<TIteratorAsyncResult> : AsyncResult<TIteratorAsyncResult>
        where TIteratorAsyncResult : IteratorAsyncResult<TIteratorAsyncResult>
    {
        static readonly Action<AsyncResult, Exception> onFinally = IteratorAsyncResult<TIteratorAsyncResult>.Finally;

        static AsyncCompletion stepCallbackDelegate;

        // DON'T make TimeoutHelper readonly field.
        // It is very unfortunate design but TimeoutHelper is a struct (value type) that is mutating. 
        // Declarating it as readonly has side impact that it prevents TimeoutHelper mutating itself causing RemainingTime() method
        // returning the original timeout value everytime.
        TimeoutHelper timeoutHelper;
        bool everCompletedAsynchronously;
        IEnumerator<AsyncStep> steps;
        Exception lastAsyncStepException;

        protected IteratorAsyncResult(TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            this.timeoutHelper = new TimeoutHelper(timeout, true);
            this.OnCompleting += IteratorAsyncResult<TIteratorAsyncResult>.onFinally;
        }

        protected delegate IAsyncResult BeginCall(TIteratorAsyncResult thisPtr, TimeSpan timeout, AsyncCallback callback, object state);

        protected delegate void EndCall(TIteratorAsyncResult thisPtr, IAsyncResult ar);

        protected delegate void Call(TIteratorAsyncResult thisPtr, TimeSpan timeout);

        private enum CurrentThreadType
        {
            Synchronous,
            StartingThread,
            Callback
        }

        protected Exception LastAsyncStepException
        {
            get { return this.lastAsyncStepException; }
            set { this.lastAsyncStepException = value; }
        }

        protected TimeSpan OriginalTimeout
        {
            get
            {
                return this.timeoutHelper.OriginalTimeout;
            }
        }

        private static AsyncCompletion StepCallbackDelegate
        {
            get
            {
                // The race here is intentional and harmless.
                if (stepCallbackDelegate == null)
                {
                    stepCallbackDelegate = new AsyncCompletion(StepCallback);
                }

                return stepCallbackDelegate;
            }
        }

        // This is typically called at the end of the derived AsyncResult
        // constructor, to start the async operation.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
        public IAsyncResult Start()
        {
            Debug.Assert(this.steps == null, "IteratorAsyncResult.Start called twice");
            try
            {
                this.steps = this.GetAsyncSteps();

                this.EnumerateSteps(CurrentThreadType.StartingThread);
            }
            catch (Exception e)
            {
                this.Complete(e);
            }

            return this;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
        public void RunSynchronously()
        {
            Debug.Assert(this.steps == null, "IteratorAsyncResult.RunSynchronously or .Start called twice");
            try
            {
                this.steps = this.GetAsyncSteps();
                this.EnumerateSteps(CurrentThreadType.Synchronous);
            }
            catch (Exception e)
            {
                this.Complete(e);
            }

            IteratorAsyncResult<TIteratorAsyncResult>.End<IteratorAsyncResult<TIteratorAsyncResult>>(this);
        }

        // Utility method to be called from GetAsyncSteps.  To create an implementation
        // of IAsyncCatch, use the CatchAndTransfer or CatchAndContinue methods.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Do not want to alter functionality.")]
        protected AsyncStep CallAsync(BeginCall beginCall, EndCall endCall, Call call, ExceptionPolicy policy)
        {
            return new AsyncStep(beginCall, endCall, call, policy);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Do not want to alter functionality.")]
        protected AsyncStep CallAsync(BeginCall beginCall, EndCall endCall, ExceptionPolicy policy)
        {
            return new AsyncStep(beginCall, endCall, null, policy);
        }

        protected AsyncStep CallParallelAsync<TWorkItem>(ICollection<TWorkItem> workItems, BeginCall<TWorkItem> beginCall, EndCall<TWorkItem> endCall, ExceptionPolicy policy)
        {
            return this.CallAsync(
                (thisPtr, t, c, s) => new ParallelAsyncResult<TWorkItem>(thisPtr, workItems, beginCall, endCall, t, c, s),
                (thisPtr, r) => ParallelAsyncResult<TWorkItem>.End(r),
                policy);
        }

        protected AsyncStep CallParallelAsync<TWorkItem>(ICollection<TWorkItem> workItems, BeginCall<TWorkItem> beginCall, EndCall<TWorkItem> endCall, TimeSpan timeout, ExceptionPolicy policy)
        {
            return this.CallAsync(
                (thisPtr, t, c, s) => new ParallelAsyncResult<TWorkItem>(thisPtr, workItems, beginCall, endCall, timeout, c, s),
                (thisPtr, r) => ParallelAsyncResult<TWorkItem>.End(r),
                policy);
        }

        protected AsyncStep CallAsyncSleep(TimeSpan amountToSleep)
        {
            Debug.Assert(amountToSleep != TimeSpan.MaxValue, "IteratorAsyncResult cannot delay for TimeSpan.MaxValue!");

            return this.CallAsync(
                (thisPtr, t, c, s) => new SleepAsyncResult(amountToSleep, c, s),
                (thisPtr, r) => SleepAsyncResult.End(r),
                (thisPtr, t) => Thread.Sleep(amountToSleep),
                ExceptionPolicy.Transfer);
        }

        protected TimeSpan RemainingTime()
        {
            return this.timeoutHelper.RemainingTime();
        }

        // The derived AsyncResult implements this method as a C# iterator.
        // The implementation should make no blocking calls.  Instead, it
        // runs synchronous code and can "yield return" the result of calling
        // "CallAsync" to cause an async method invocation.
        protected abstract IEnumerator<AsyncStep> GetAsyncSteps();

        protected void Complete(Exception operationException)
        {
            this.Complete(!this.everCompletedAsynchronously, operationException);
        }

        static bool StepCallback(IAsyncResult result)
        {
            var thisPtr = (IteratorAsyncResult<TIteratorAsyncResult>)result.AsyncState;

            bool syncContinue = thisPtr.CheckSyncContinue(result);

            if (!syncContinue)
            {
                thisPtr.everCompletedAsynchronously = true;

                try
                {
                    // Don't refactor this into a seperate method. It adds one extra call stack reducing readibility of call stack in trace.
                    thisPtr.steps.Current.EndCall((TIteratorAsyncResult)thisPtr, result);
                }
                catch (Exception e)
                {
                    if (!thisPtr.HandleException(e))
                    {
                        throw;
                    }
                }

                thisPtr.EnumerateSteps(CurrentThreadType.Callback);
            }

            return syncContinue;
        }

        static void Finally(AsyncResult result, Exception exception)
        {
            var thisPtr = (IteratorAsyncResult<TIteratorAsyncResult>)result;
            try
            {
                IEnumerator<AsyncStep> steps = thisPtr.steps;
                if (steps != null)
                {
                    steps.Dispose();
                }
            }
            catch (Exception)
            {
                if (exception == null)
                {
                    throw;
                }
            }
        }

        bool MoveNextStep()
        {
            return this.steps.MoveNext();
        }

        // This runs async steps until one of them completes asynchronously, or until
        // Begin throws on the Start thread with a policy of PassThrough.
        void EnumerateSteps(CurrentThreadType state)
        {
            while (!this.IsCompleted && this.MoveNextStep())
            {
                this.LastAsyncStepException = null;
                AsyncStep step = this.steps.Current;
                if (step.BeginCall != null)
                {
                    IAsyncResult result = null;

                    if (state == CurrentThreadType.Synchronous && step.HasSynchronous)
                    {
                        if (step.Policy == ExceptionPolicy.Transfer)
                        {
                            step.Call((TIteratorAsyncResult)this, this.timeoutHelper.RemainingTime());
                        }
                        else
                        {
                            try
                            {
                                step.Call((TIteratorAsyncResult)this, this.timeoutHelper.RemainingTime());
                            }
                            catch (Exception e)
                            {
                                if (!this.HandleException(e))
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (step.Policy == ExceptionPolicy.Transfer)
                        {
                            result = step.BeginCall(
                                (TIteratorAsyncResult)this,
                                this.timeoutHelper.RemainingTime(),
                                this.PrepareAsyncCompletion(IteratorAsyncResult<TIteratorAsyncResult>.StepCallbackDelegate),
                                this);
                        }
                        else
                        {
                            try
                            {
                                result = step.BeginCall(
                                    (TIteratorAsyncResult)this,
                                    this.timeoutHelper.RemainingTime(),
                                    this.PrepareAsyncCompletion(IteratorAsyncResult<TIteratorAsyncResult>.StepCallbackDelegate),
                                    this);
                            }
                            catch (Exception e)
                            {
                                if (!this.HandleException(e))
                                {
                                    throw;
                                }
                            }
                        }
                    }

                    if (result != null)
                    {
                        if (!this.CheckSyncContinue(result))
                        {
                            return;
                        }

                        try
                        {
                            // Don't refactor this into a seperate method. It adds one extra call stack reducing readibility of call stack in trace.
                            this.steps.Current.EndCall((TIteratorAsyncResult)this, result);
                        }
                        catch (Exception e)
                        {
                            if (!this.HandleException(e))
                            {
                                throw;
                            }
                        }
                    }
                }
            }

            if (!this.IsCompleted)
            {
                this.Complete(!this.everCompletedAsynchronously);
            }
        }

        // Returns true if a handler matched the Exception, false otherwise.
        bool HandleException(Exception e)
        {
            bool handled;

            this.LastAsyncStepException = e;
            AsyncStep step = this.steps.Current;

            switch (step.Policy)
            {
                case ExceptionPolicy.Continue:
                    handled = true;
                    break;
                case ExceptionPolicy.Transfer:
                    handled = false;
                    if (!this.IsCompleted)
                    {
                        this.Complete(e);
                        handled = true;
                    }
                    break;
                default:
                    handled = false;
                    break;
            }

            return handled;
        }

        [DebuggerStepThrough]
        protected struct AsyncStep
        {
            readonly ExceptionPolicy policy;
            readonly BeginCall beginCall;
            readonly EndCall endCall;
            readonly Call call;

            public static readonly AsyncStep Empty = new AsyncStep();

            public AsyncStep(
                BeginCall beginCall,
                EndCall endCall,
                Call call,
                ExceptionPolicy policy)
            {
                this.policy = policy;
                this.beginCall = beginCall;
                this.endCall = endCall;
                this.call = call;
            }


            public BeginCall BeginCall
            {
                get { return this.beginCall; }
            }

            public EndCall EndCall
            {
                get { return this.endCall; }
            }

            public Call Call
            {
                get { return this.call; }
            }

            public bool HasSynchronous
            {
                get
                {
                    return this.call != null;
                }
            }

            public ExceptionPolicy Policy
            {
                get
                {
                    return this.policy;
                }
            }
        }

        protected enum ExceptionPolicy
        {
            Transfer,
            Continue
        }

        sealed class SleepAsyncResult : AsyncResult<SleepAsyncResult>
        {
            readonly static Action<object> onTimer = new Action<object>(OnTimer);
            readonly IOThreadTimer timer;

            public SleepAsyncResult(TimeSpan amount, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.timer = new IOThreadTimer(onTimer, this, false);
                this.timer.Set(amount);
            }

            static void OnTimer(object state)
            {
                SleepAsyncResult thisPtr = (SleepAsyncResult)state;
                thisPtr.Complete(false);
            }
        }

        protected delegate IAsyncResult BeginCall<TWorkItem>(TIteratorAsyncResult thisPtr, TWorkItem workItem, TimeSpan timeout, AsyncCallback callback, object state);

        protected delegate void EndCall<TWorkItem>(TIteratorAsyncResult thisPtr, TWorkItem workItem, IAsyncResult ar);

        sealed class ParallelAsyncResult<TWorkItem> : AsyncResult<ParallelAsyncResult<TWorkItem>>
        {
            static AsyncCallback completed = new AsyncCallback(OnCompleted);

            readonly TIteratorAsyncResult iteratorAsyncResult;
            readonly ICollection<TWorkItem> workItems;
            readonly EndCall<TWorkItem> endCall;
            long actions;
            Exception firstException;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
            public ParallelAsyncResult(TIteratorAsyncResult iteratorAsyncResult, ICollection<TWorkItem> workItems, BeginCall<TWorkItem> beginCall, EndCall<TWorkItem> endCall, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.iteratorAsyncResult = iteratorAsyncResult;
                this.workItems = workItems;
                this.endCall = endCall;
                this.actions = this.workItems.Count + 1;

                foreach (TWorkItem source in workItems)
                {
                    try
                    {
                        beginCall(iteratorAsyncResult, source, timeout, completed, new CallbackState(this, source));
                    }
                    catch (Exception e)
                    {
                        TryComplete(e, true);
                    }
                }

                TryComplete(null, true);
            }

            void TryComplete(Exception exception, bool completedSynchronously)
            {
                if (this.firstException == null)
                {
                    this.firstException = exception;
                }

                if (Interlocked.Decrement(ref this.actions) == 0)
                {
                    this.Complete(completedSynchronously, this.firstException);
                }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
            static void OnCompleted(IAsyncResult ar)
            {
                CallbackState state = (CallbackState)ar.AsyncState;
                ParallelAsyncResult<TWorkItem> thisPtr = state.AsyncResult;

                try
                {
                    thisPtr.endCall(thisPtr.iteratorAsyncResult, state.AsyncData, ar);
                    thisPtr.TryComplete(null, false);
                }
                catch (Exception e)
                {
                    thisPtr.TryComplete(e, false);
                }
            }

            sealed class CallbackState
            {
                public CallbackState(ParallelAsyncResult<TWorkItem> asyncResult, TWorkItem data)
                {
                    this.AsyncResult = asyncResult;
                    this.AsyncData = data;
                }

                public ParallelAsyncResult<TWorkItem> AsyncResult { get; private set; }

                public TWorkItem AsyncData { get; private set; }
            }
        }
    }
}
