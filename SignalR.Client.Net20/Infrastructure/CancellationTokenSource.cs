using System;

namespace SignalR.Client.Net20.Infrastructure
{
    public class CancellationTokenSource
    {
        private volatile bool _canceled;

        public void Cancel()
        {
            _canceled = true;
        }

        public bool IsCancellationRequested
        {
            get { return _canceled; }
        }
    }

    public class TaskCompletionSource<T>
    {
        private readonly Task _internalTask = new Task(); 

        public void SetException(Exception exception)
        {
            _internalTask.OnFinished(null,exception);
        }

        public void SetResult(T result)
        {
            _internalTask.OnFinished(result,null);
        }

        public Task Task
        {
            get { return _internalTask; }
        }

        public void SetCanceled()
        {
            _internalTask.OnFinished(null,null);
        }

        public void TrySetException(Exception exception)
        {
            SetException(exception);
        }

        public void TrySetResult(T result)
        {
            SetResult(result);
        }
    }
}
