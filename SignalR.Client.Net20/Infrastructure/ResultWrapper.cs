using System;

namespace SignalR.Client.Net20.Infrastructure
{
    /// <summary>
    /// A result wrapper adding details from the operation.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    public class ResultWrapper<T>
    {
        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// Gets or sets the indication if the task execution ended up with an exception.
        /// </summary>
        public bool IsFaulted { get; set; }

        /// <summary>
        /// Gets or sets the exception details.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the indication if this task is cancelled.
        /// </summary>
        public bool IsCanceled { get; set; }
    }
}