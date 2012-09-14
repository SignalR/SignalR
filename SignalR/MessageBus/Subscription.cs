using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    internal class Subscription : IDisposable
    {
        private List<Cursor> _cursors;
        private readonly Func<MessageResult, Task<bool>> _callback;
        private readonly int _maxMessages;

        private readonly PerformanceCounter _subsTotalCounter;
        private readonly PerformanceCounter _subsCurrentCounter;
        private readonly PerformanceCounter _subsPerSecCounter;

        private readonly object _lockObj = new object();
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

        public IList<Cursor> Cursors
        {
            get
            {
                return _cursors;
            }
        }

        public string Identity { get; private set; }

        public Subscription(string identity, IEnumerable<Cursor> cursors, Func<MessageResult, Task<bool>> callback, int maxMessages, IPerformanceCounterWriter counters)
        {
            Identity = identity;
            _cursors = new List<Cursor>(cursors);
            _callback = callback;
            _maxMessages = maxMessages;
            _subsTotalCounter = counters.GetCounter(PerformanceCounters.MessageBusSubscribersTotal);
            _subsCurrentCounter = counters.GetCounter(PerformanceCounters.MessageBusSubscribersCurrent);
            _subsPerSecCounter = counters.GetCounter(PerformanceCounters.MessageBusSubscribersPerSec);

            _subsTotalCounter.SafeIncrement();
            _subsCurrentCounter.SafeIncrement();
            _subsPerSecCounter.SafeIncrement();
        }

        public Task<bool> Invoke(MessageResult result)
        {
            return _callback.Invoke(result);
        }

        public Task WorkAsync(ConcurrentDictionary<string, Topic> topics)
        {
            if (SetWorking())
            {
                var tcs = new TaskCompletionSource<object>();


                WorkImpl(topics, tcs);

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

        private void WorkImpl(ConcurrentDictionary<string, Topic> topics, TaskCompletionSource<object> taskCompletionSource)
        {
        Process:
            int totalCount = 0;
            string nextCursor = null;
            List<ArraySegment<Message>> items = null;
            var cursors = new List<Cursor>();

            if (!Alive)
            {
                // If this subscription is dead then return immediately
                taskCompletionSource.TrySetResult(null);
                return;
            }

            lock (_lockObj)
            {
                items = new List<ArraySegment<Message>>(Cursors.Count);
                for (int i = 0; i < Cursors.Count; i++)
                {
                    Cursor cursor = Cursor.Clone(Cursors[i]);
                    cursors.Add(cursor);

                    MessageStoreResult<Message> storeResult = cursor.Topic.Store.GetMessages(cursor.Id, _maxMessages);
                    ulong next = storeResult.FirstMessageId + (ulong)storeResult.Messages.Count;

                    cursor.Id = next;

                    if (storeResult.Messages.Count > 0)
                    {
                        items.Add(storeResult.Messages);
                        totalCount += storeResult.Messages.Count;
                    }
                }

                nextCursor = Cursor.MakeCursor(cursors);
            }

            if (Alive && items.Count > 0)
            {
                lock (_lockObj)
                {
                    _cursors = cursors;
                    cursors = null;
                }

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
                    WorkImplAsync(callbackTask, topics, taskCompletionSource);
                }
            }
            else
            {
                taskCompletionSource.TrySetResult(null);
            }
        }

        private void WorkImplAsync(Task<bool> callbackTask, ConcurrentDictionary<string, Topic> topics, TaskCompletionSource<object> taskCompletionSource)
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
                    WorkImpl(topics, taskCompletionSource);
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

        public bool AddOrUpdateCursor(string key, ulong id, Topic topic)
        {
            lock (_lockObj)
            {
                // O(n), but small n and it's not common
                var index = _cursors.FindIndex(c => c.Key == key);
                if (index == -1)
                {
                    _cursors.Add(new Cursor
                    {
                        Key = key,
                        Id = id,
                        Topic = topic
                    });

                    return true;
                }

                return false;
            }
        }

        public bool UpdateCursor(string key, ulong id)
        {
            lock (_lockObj)
            {
                // O(n), but small n and it's not common
                var index = _cursors.FindIndex(c => c.Key == key);
                if (index != -1)
                {
                    _cursors[index].Id = id;
                    return true;
                }

                return false;
            }
        }

        public void RemoveCursor(string eventKey)
        {
            lock (_lockObj)
            {
                _cursors.RemoveAll(c => c.Key == eventKey);
            }
        }

        public void SetCursorTopic(string key, Topic topic)
        {
            lock (_lockObj)
            {
                // O(n), but small n and it's not common
                var index = _cursors.FindIndex(c => c.Key == key);
                if (index != -1)
                {
                    _cursors[index].Topic = topic;
                }
            }
        }

        public void Dispose()
        {
            // REVIEW: Should we make this block if there's pending work
            Interlocked.Exchange(ref _disposed, 1);

            _subsCurrentCounter.SafeDecrement();
            _subsPerSecCounter.SafeDecrement();
        }

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
