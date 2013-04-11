// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Hosting.Memory
{

    // From http://msdn.microsoft.com/en-us/magazine/cc163467.aspx
    internal class AsyncResult : IAsyncResult
    {
        // Fields set at construction which never change while 
        // operation is pending
        readonly AsyncCallback m_AsyncCallback;
        readonly Object m_AsyncState;

        // Fields set at construction which do change after 
        // operation completes
        const Int32 c_StatePending = 0;
        const Int32 c_StateCompletedSynchronously = 1;
        const Int32 c_StateCompletedAsynchronously = 2;
        Int32 m_CompletedState = c_StatePending;

        // Field that may or may not get set depending on usage
        ManualResetEvent m_AsyncWaitHandle;

        // Fields set when operation completes
        Exception m_exception;

        public AsyncResult(AsyncCallback asyncCallback, Object state)
        {
            m_AsyncCallback = asyncCallback;
            m_AsyncState = state;
        }

        public void SetAsCompleted(
            Exception exception, Boolean completedSynchronously)
        {
            // Passing null for exception means no error occurred. 
            // This is the common case
            m_exception = exception;

            // The m_CompletedState field MUST be set prior calling the callback
            Int32 prevState = Interlocked.Exchange(ref m_CompletedState,
                completedSynchronously
                    ? c_StateCompletedSynchronously
                    : c_StateCompletedAsynchronously);

            if (prevState != c_StatePending)
            {
                // Noop
                return;
            }

            // If the event exists, set it
            if (m_AsyncWaitHandle != null) m_AsyncWaitHandle.Set();

            // If a callback method was set, call it
            if (m_AsyncCallback != null) m_AsyncCallback(this);
        }

        public void EndInvoke()
        {
            // This method assumes that only 1 thread calls EndInvoke 
            // for this object
            if (!IsCompleted)
            {
                // If the operation isn't done, wait for it
                AsyncWaitHandle.WaitOne();
                AsyncWaitHandle.Close();
                m_AsyncWaitHandle = null; // Allow early GC
            }

            // Operation is done: if an exception occured, throw it
            if (m_exception != null) throw m_exception;
        }

        #region Implementation of IAsyncResult

        public Object AsyncState
        {
            get { return m_AsyncState; }
        }

        public Boolean CompletedSynchronously
        {
            get
            {
                return Thread.VolatileRead(ref m_CompletedState) ==
                    c_StateCompletedSynchronously;
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "It's disposed elsewhere")]
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (m_AsyncWaitHandle == null)
                {
                    Boolean done = IsCompleted;
                    ManualResetEvent mre = new ManualResetEvent(done);
                    if (Interlocked.CompareExchange(ref m_AsyncWaitHandle,
                        mre, null) != null)
                    {
                        // Another thread created this object's event; dispose 
                        // the event we just created
                        mre.Close();
                    }
                    else
                    {
                        if (!done && IsCompleted)
                        {
                            // If the operation wasn't done when we created 
                            // the event but now it is done, set the event
                            m_AsyncWaitHandle.Set();
                        }
                    }
                }
                return m_AsyncWaitHandle;
            }
        }

        public Boolean IsCompleted
        {
            get
            {
                return Thread.VolatileRead(ref m_CompletedState) !=
                    c_StatePending;
            }
        }

        #endregion
    }

    internal class AsyncResult<TResult> : AsyncResult
    {
        // Field set when operation completes
        TResult m_result = default(TResult);
        IDisposable registration = null;

        public AsyncResult(AsyncCallback asyncCallback, Object state, CancellationToken token) :
            base(asyncCallback, state)
        {
            registration = token.Register(Cancel);
        }

        private void Cancel()
        {
            SetAsCompleted(new OperationCanceledException(), completedSynchronously: false);
        }

        public void SetAsCompleted(TResult result,
            Boolean completedSynchronously)
        {
            // Save the asynchronous operation's result
            m_result = result;

            // Tell the base class that the operation completed 
            // sucessfully (no exception)
            base.SetAsCompleted(null, completedSynchronously);
        }

        public new TResult EndInvoke()
        {
            base.EndInvoke(); // Wait until operation has completed 
            
            if (registration != null)
            {
                registration.Dispose();
            }

            return m_result; // Return the result (if above didn't throw)
        }
    }

}
