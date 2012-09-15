using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public abstract class Subscription : ISubscription, IDisposable
    {
        private readonly Func<MessageResult, Task<bool>> _callback;
        private readonly int _maxMessages;

        private readonly PerformanceCounter _subsTotalCounter;
        private readonly PerformanceCounter _subsCurrentCounter;
        private readonly PerformanceCounter _subsPerSecCounter;

        private int _disposed;

        private int _queued;
        private int _working;

        private bool Alive
        {
            get
            {
                return _disposed == 0;
            }
        }

        public string Identity { get; private set; }

        public IEnumerable<string> EventKeys { get; set; }

        public int MaxMessages { get; set; }

        public Subscription(string identity, IEnumerable<string> eventKeys, Func<MessageResult, Task<bool>> callback, int maxMessages, IPerformanceCounterWriter counters)
        {
            Identity = identity;
            _callback = callback;
            _maxMessages = maxMessages;
            EventKeys = eventKeys;
            MaxMessages = maxMessages;

            _subsTotalCounter = counters.GetCounter(PerformanceCounters.MessageBusSubscribersTotal);
            _subsCurrentCounter = counters.GetCounter(PerformanceCounters.MessageBusSubscribersCurrent);
            _subsPerSecCounter = counters.GetCounter(PerformanceCounters.MessageBusSubscribersPerSec);

            _subsTotalCounter.SafeIncrement();
            _subsCurrentCounter.SafeIncrement();
            _subsPerSecCounter.SafeIncrement();
        }

        public virtual Task<bool> Invoke(MessageResult result)
        {
            return _callback.Invoke(result);
        }

        public Task WorkAsync()
        {
            if (SetWorking())
            {
                var tcs = new TaskCompletionSource<object>();


                WorkImpl(tcs);

                // Fast Path
                if (tcs.Task.IsCompleted)
                {
                    UnsetWorking();
                    return tcs.Task;
                }

                return FinishAsync(tcs);
            }

            return TaskAsyncHelper.Empty;
        }

        public bool SetQueued()
        {
            return Interlocked.Exchange(ref _queued, 1) == 0;
        }

        public bool UnsetQueued()
        {
            return Interlocked.Exchange(ref _queued, 0) == 1;
        }

        private bool SetWorking()
        {
            return Interlocked.Exchange(ref _working, 1) == 0;
        }

        private bool UnsetWorking()
        {
            return Interlocked.Exchange(ref _working, 0) == 1;
        }

        private Task FinishAsync(TaskCompletionSource<object> tcs)
        {
            return tcs.Task.ContinueWith(task =>
            {
                UnsetWorking();

                if (task.IsFaulted)
                {
                    return TaskAsyncHelper.FromError(task.Exception);
                }

                return TaskAsyncHelper.Empty;
            }).FastUnwrap();
        }

        private void WorkImpl(TaskCompletionSource<object> taskCompletionSource)
        {
        Process:
            if (!Alive)
            {
                // If this subscription is dead then return immediately
                taskCompletionSource.TrySetResult(null);
                return;
            }

            int totalCount = 0;
            string nextCursor = null;
            var items = new List<ArraySegment<Message>>();
            object state = null;

            PerformWork(ref items, out nextCursor, ref totalCount, out state);

            if (Alive && items.Count > 0)
            {
                BeforeInvoke(state);

                var messageResult = new MessageResult(items, nextCursor, totalCount);
                Task<bool> callbackTask = Invoke(messageResult);

                if (callbackTask.IsCompleted)
                {
                    try
                    {
                        // Make sure exceptions propagate
                        callbackTask.Wait();

                        if (callbackTask.Result)
                        {
                            // Sync path
                            goto Process;
                        }
                        else
                        {
                            // If we're done pumping messages through to this subscription
                            // then dispose
                            Dispose();

                            // If the callback said it's done then stop
                            taskCompletionSource.TrySetResult(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.TrySetException(ex);
                    }
                }
                else
                {
                    WorkImplAsync(callbackTask, taskCompletionSource);
                }
            }
            else
            {
                taskCompletionSource.TrySetResult(null);
            }
        }

        protected virtual void BeforeInvoke(object state)
        {
        }

        protected abstract void PerformWork(ref List<ArraySegment<Message>> items, out string nextCursor, ref int totalCount, out object state);

        private void WorkImplAsync(Task<bool> callbackTask, TaskCompletionSource<object> taskCompletionSource)
        {
            // Async path
            callbackTask.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    taskCompletionSource.TrySetException(task.Exception);
                }
                else if (task.Result)
                {
                    WorkImpl(taskCompletionSource);
                }
                else
                {
                    // If we're done pumping messages through to this subscription
                    // then dispose
                    Dispose();

                    // If the callback said it's done then stop
                    taskCompletionSource.TrySetResult(null);
                }
            });
        }

        public abstract bool AddEvent(string key, Topic topic);

        public abstract void RemoveEvent(string eventKey);

        public abstract void SetEventTopic(string key, Topic topic);

        public void Dispose()
        {
            // REVIEW: Should we make this block if there's pending work
            Interlocked.Exchange(ref _disposed, 1);

            _subsCurrentCounter.SafeDecrement();
            _subsPerSecCounter.SafeDecrement();
        }

        public abstract string GetCursor();

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Identity.Equals(((Subscription)obj).Identity);
        }
    }
}
