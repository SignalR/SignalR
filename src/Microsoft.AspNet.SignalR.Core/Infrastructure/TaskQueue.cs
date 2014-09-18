// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

#if CLIENT_NET45 || CLIENT_NET4 || PORTABLE || NETFX_CORE
#define CLIENT
#endif

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

#if CLIENT
using Microsoft.AspNet.SignalR.Client.Infrastructure;
#endif

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

#if CLIENT
        // This is the TaskQueueMonitor in the .NET client that watches for
        // suspected deadlocks in user code.
        private readonly ITaskMonitor _taskMonitor;
#endif

        public TaskQueue()
            : this(TaskAsyncHelper.Empty)
        {
        }

        public TaskQueue(Task initialTask)
        {
            _lastQueuedTask = initialTask;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]        
        public TaskQueue(Task initialTask, int maxSize)
        {
            _lastQueuedTask = initialTask;
            _maxSize = maxSize;
        }


#if CLIENT
        public TaskQueue(Task initialTask, ITaskMonitor taskMonitor)
            : this(initialTask)
        {
            _taskMonitor = taskMonitor;
        }
#endif

#if !CLIENT
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
                    // Increment the size if the queue
                    if (Interlocked.Increment(ref _size) > _maxSize)
                    {
                        Interlocked.Decrement(ref _size);

                        // We failed to enqueue because the size limit was reached
                        return null;
                    }

#if !CLIENT
                    var counter = QueueSizeCounter;
                    if (counter != null)
                    {
                        counter.Increment();
                    }
#endif
                }

                var newTask = _lastQueuedTask.Then((n, ns, q) => q.InvokeNext(n, ns), taskFunc, state, this);

                _lastQueuedTask = newTask;
                return newTask;
            }
        }

        private Task InvokeNext(Func<object, Task> next, object nextState)
        {
#if CLIENT
            if (_taskMonitor != null)
            {
                _taskMonitor.TaskStarted();
            }
#endif

            return next(nextState).Finally(s => ((TaskQueue)s).Dequeue(), this);
        }

        private void Dequeue()
        {
#if CLIENT
            if (_taskMonitor != null)
            {
                _taskMonitor.TaskCompleted();
            }
#endif

            if (_maxSize != null)
            {
                // Decrement the number of items left in the queue
                Interlocked.Decrement(ref _size);

#if !CLIENT
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
