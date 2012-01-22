using System;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR
{
    internal sealed class TaskWrapperAsyncResult : IAsyncResult
    {
        internal TaskWrapperAsyncResult(Task task, object asyncState)
        {
            Task = task;
            AsyncState = asyncState;
        }

        public object AsyncState
        {
            get;
            private set;
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return ((IAsyncResult)Task).AsyncWaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return ((IAsyncResult)Task).CompletedSynchronously; }
        }

        public bool IsCompleted
        {
            get { return ((IAsyncResult)Task).IsCompleted; }
        }

        public Task Task
        {
            get;
            private set;
        }
    }
}
