// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
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

        public TaskQueue()
            : this(TaskAsyncHelper.Empty)
        {
        }

        public TaskQueue(Task initialTask)
        {
            _lastQueuedTask = initialTask;
        }

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

                Task newTask = _lastQueuedTask.Then((next, nextState) => next(nextState), taskFunc, state);
                _lastQueuedTask = newTask;
                return newTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is shared code")]
        public Task Enqueue(Func<Task> taskFunc)
        {
            // Lock the object for as short amount of time as possible
            lock (_lockObj)
            {
                if (_drained)
                {
                    return _lastQueuedTask;
                }

                Task newTask = _lastQueuedTask.Then(next => next(), taskFunc);
                _lastQueuedTask = newTask;
                return newTask;
            }
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
