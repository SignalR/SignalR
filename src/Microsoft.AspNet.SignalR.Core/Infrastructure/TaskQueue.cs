﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    // Allows serial queuing of Task instances
    // The tasks are not called on the current synchronization context

    internal sealed class TaskQueue
    {
        private readonly object _lockObj = new object();
        private Task _lastQueuedTask = TaskAsyncHelper.Empty;
        private volatile bool _drained;

        public bool IsDrained
        {
            get
            {
                return _drained;
            }
        }

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
