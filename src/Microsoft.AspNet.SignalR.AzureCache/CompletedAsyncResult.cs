using System;
using System.Threading;

namespace Microsoft.AspNet.SignalR
{
    internal sealed class CompletedAsyncResult<T> : IAsyncResult
    {
        readonly T _data;

        public CompletedAsyncResult(T data)
        {
            _data = data;
        }

        public T Data
        {
            get { return _data; }
        }

        #region IAsyncResult Members

        public object AsyncState
        {
            get { return _data; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public bool CompletedSynchronously
        {
            get { return true; }
        }

        public bool IsCompleted
        {
            get { return true; }
        }
        #endregion
    }
}