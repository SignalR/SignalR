using System.Collections.Generic;

namespace SignalR.Hubs
{
    /// <summary>
    /// The response returned from an incoming hub request.
    /// </summary>
    public class HubResponse
    {
        /// <summary>
        /// The changes made the the round tripped state.
        /// </summary>
        public IDictionary<string, object> State { get; set; }

        /// <summary>
        /// The result of the invocation.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// The id of the operation.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The exception that occurs as a result of invoking the hub method.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The stack trace of the exception that occurs as a result of invoking the hub method.
        /// </summary>
        public string StackTrace { get; set; }
    }
}
