using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SignalR.Client.Hubs
{
    /// <summary>
    /// Represents the result of a hub invocation.
    /// </summary>
    /// <typeparam name="T">The return type of the hub.</typeparam>
    public class HubResult<T>
    {
        /// <summary>
        /// The return value of the hub
        /// </summary>
        public T Result { get; set; }
        
        /// <summary>
        /// The error message returned from the hub invocation.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The caller state from this hub.
        /// </summary>
        public IDictionary<string, JToken> State { get; set; }
    }
}
