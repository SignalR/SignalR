namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Threading;

    sealed class CompletedAsyncResult<TResult> : IAsyncResult
    {
        readonly ManualResetEvent asyncWaitHandle;
        readonly object asyncState;
        readonly TResult result;

        public CompletedAsyncResult(TResult result, AsyncCallback callback, object state)
        {
            this.result = result;
            this.asyncWaitHandle = new ManualResetEvent(true);
            this.asyncState = state;

            if (callback != null)
            {
                callback(this);
            }
        }

        public TResult Result
        {
            get { return this.result; }
        }

        public object AsyncState
        {
            get { return this.asyncState; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return this.asyncWaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return true; }
        }

        public bool IsCompleted
        {
            get { return true; }
        }

        public static TResult End(IAsyncResult asyncResult)
        {
            CompletedAsyncResult<TResult> completedAsyncResult = (CompletedAsyncResult<TResult>)asyncResult;
            completedAsyncResult.AsyncWaitHandle.Close();
            return completedAsyncResult.Result;
        }
    }
}
