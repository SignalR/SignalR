﻿using System;
using System.Threading.Tasks;

namespace SignalR.Hosting.AspNet.Infrastructure
{
    // Allows serial queuing of Task instances
    // The tasks are not called on the current synchronization context

    internal sealed class TaskQueue
    {
        private readonly object _lockObj = new object();
        private Task _lastQueuedTask = Task.FromResult<int>(0);

        public Task Enqueue(Func<Task> taskFunc)
        {
            // Lock the object for as short amount of time as possible

            lock (_lockObj)
            {
                Task newTask = _lastQueuedTask.ContinueWith(_ => taskFunc(), TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
                _lastQueuedTask = newTask;
                return newTask;
            }
        }

    }
}
