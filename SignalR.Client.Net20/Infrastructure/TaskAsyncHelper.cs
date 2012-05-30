using System;
using System.Threading;

namespace SignalR.Client.Net20.Infrastructure
{
    /// <summary>
    /// Static class with helper functions for the custom implementation of tasks.
    /// </summary>
    public static class TaskAsyncHelper
    {
        /// <summary>
        /// Create a task that is delayed by the given time span.
        /// </summary>
        /// <param name="timeSpan">The time span to delay subsequent operations with.</param>
        /// <returns>A non-generic task.</returns>
        public static Task Delay(TimeSpan timeSpan)
        {
            var newEvent = new Task();
            using (var resetEvent = new ManualResetEvent(false))
            {
                resetEvent.WaitOne(timeSpan);
            }
            newEvent.OnFinished(null, null);
            return newEvent;
        }

        /// <summary>
        /// Returns an empty task that is done.
        /// </summary>
        public static Task Empty
        {
            get
            {
                var task = new Task();
                task.OnFinished(null,null);
                return task;
            }
        }
    }
}