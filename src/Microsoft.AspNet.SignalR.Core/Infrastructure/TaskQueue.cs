// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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

        public Task Enqueue(Func<Task> taskFunc)
        {
            // Lock the object for as short amount of time as possible

            lock (_lockObj)
            {
#if NET45
                Task newTask = _lastQueuedTask.ContinueWith(_ => taskFunc(), TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
#else
                Task newTask = _lastQueuedTask.Then(next => next(), taskFunc);
#endif
                _lastQueuedTask = newTask;
                return newTask;
            }
        }

    }
}
