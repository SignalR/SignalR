using System;

namespace SignalR.Client.Net20.Infrastructure
{
    /// <summary>
    /// Custom result event args adding the ResultWrapper of the correct type for the event.
    /// </summary>
    /// <typeparam name="T">The type of the result from the operation.</typeparam>
    public class CustomResultArgs<T> : EventArgs
    {
        public ResultWrapper<T> ResultWrapper { get; set; }
    }
}