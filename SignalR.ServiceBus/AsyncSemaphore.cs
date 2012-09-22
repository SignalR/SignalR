namespace SignalR.ServiceBus
{
    using System;
    using System.Collections.Generic;

    sealed class AsyncSemaphore
    {
        readonly object syncRoot;
        readonly int maxCount;
        readonly Queue<IWaiter> waiters;
        int count;

        public AsyncSemaphore(int maxCount)
        {
            this.syncRoot = new object();
            this.maxCount = maxCount;
            this.waiters = new Queue<IWaiter>();
        }

        public bool TryEnter()
        {
            lock (this.syncRoot)
            {
                if (this.count < this.maxCount)
                {
                    this.count++;
                    this.waiters.Enqueue(new SyncWaiter());
                    return true;
                }
            }

            return false;
        }

        public IAsyncResult BeginEnter(AsyncCallback callback, object state)
        {
            lock (this.syncRoot)
            {
                if (this.count < this.maxCount)
                {
                    this.count++;
                    return new CompletedAsyncResult<bool>(true, callback, state);
                }

                AsyncWaiter asyncWaiter = new AsyncWaiter(callback, state);
                this.waiters.Enqueue(asyncWaiter);
                return asyncWaiter;
            }
        }

        public bool EndEnter(IAsyncResult asyncResult)
        {
            AsyncWaiter asyncWaiter = asyncResult as AsyncWaiter;
            if (asyncWaiter != null)
            {
                return AsyncWaiter.End(asyncResult);
            }

            return CompletedAsyncResult<bool>.End(asyncResult);
        }

        public void Exit()
        {
            IWaiter waiter;

            lock (this.syncRoot)
            {
                if (this.count == 0)
                {
                    throw new InvalidOperationException(
                        "Semaphore's count has already reached zero before this operaiton. Make sure Exit() is called only after successfully entered the semaphore");
                }

                waiter = this.waiters.Dequeue();
                this.count--;
            }

            waiter.Signal();
        }

        interface IWaiter
        {
            void Signal();
        }

        sealed class SyncWaiter : IWaiter
        {
            public SyncWaiter()
            {
            }

            public void Signal()
            {
            }
        }

        sealed class AsyncWaiter : AsyncResult<AsyncWaiter>, IWaiter
        {
            bool result;

            public AsyncWaiter(AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.result = true;
            }

            public new static bool End(IAsyncResult asyncResult)
            {
                return AsyncResult<AsyncWaiter>.End(asyncResult).result;
            }

            public void Signal()
            {
                //this.result = !isAborted;
                IOThreadScheduler.ScheduleCallbackNoFlow(state => ((AsyncWaiter)state).Complete(false), this);
            }
        }
    }
}
