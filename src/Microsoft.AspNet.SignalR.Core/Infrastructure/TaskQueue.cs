// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    // Allows serial queuing of Task instances
    // The tasks are not called on the current synchronization context

    internal sealed class TaskQueue
    {
        private readonly object _lockObj = new object();
        private Task _lastQueuedTask;
        private volatile bool _drained;
        private readonly int? _maxSize;
        private long _size;
        private Action<object> _dequeueAction;
        private Action<Func<object, Task>, object> _invokeNextAction;

        public TaskQueue()
            : this(TaskAsyncHelper.Empty)
        {
        }

        public TaskQueue(Task initialTask)
        {
            _lastQueuedTask = initialTask;
            _dequeueAction = queue => Dequeue(queue);
            _invokeNextAction = (next, nextState) => InvokeNext(next, nextState);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]        
        public TaskQueue(Task initialTask, int maxSize)
        {
            _lastQueuedTask = initialTask;
            _maxSize = maxSize;
            _dequeueAction = queue => Dequeue(queue);
            _invokeNextAction = (next, nextState) => InvokeNext(next, nextState);
        }

#if !CLIENT_NET45
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code.")]
        public IPerformanceCounter QueueSizeCounter { get; set; }
#endif

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]
        public bool IsDrained
        {
            get
            {
                return _drained;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]
        public Task Enqueue(Func<object, Task> taskFunc, object state)
        {
            // Lock the object for as short amount of time as possible
            lock (_lockObj)
            {
                if (_drained)
                {
                    return _lastQueuedTask;
                }

                if (_maxSize != null)
                {
                    if (Interlocked.Read(ref _size) == _maxSize)
                    {
                        // REVIEW: Do we need to make the contract more clear between the
                        // queue full case and the queue drained case? Should we throw an exeception instead?
                        
                        // We failed to enqueue because the size limit was reached
                        return null;
                    }

                    // Increment the size if the queue
                    Interlocked.Increment(ref _size);
                    
#if !CLIENT_NET45
                    var counter = QueueSizeCounter;
                    if (counter != null)
                    {
                        counter.Increment();
                    }
#endif
                }

                Task newTask = _lastQueuedTask.Then(_invokeNextAction, taskFunc, state);
                _lastQueuedTask = newTask;
                return newTask;
            }
        }

        private Task InvokeNext(Func<object, Task> next, object nextState)
        {
            return next(nextState).Finally(_dequeueAction, this);
        }

#if !CLIENT_NET45
        private void Dequeue(object state)
#else
        private static void Dequeue(object state)
#endif
        {
            var queue = (TaskQueue)state;
            if (queue._maxSize != null)
            {
                // Decrement the number of items left in the queue
                Interlocked.Decrement(ref queue._size);

#if !CLIENT_NET45
                var counter = QueueSizeCounter;
                if (counter != null)
                {
                    counter.Decrement();
                }
#endif
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]
        public Task Enqueue(Func<Task> taskFunc)
        {
            return Enqueue(state => ((Func<Task>)state).Invoke(), taskFunc);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]
        public Task Drain()
        {
            lock (_lockObj)
            {
                _drained = true;

                return _lastQueuedTask;
            }
        }
    }
}
