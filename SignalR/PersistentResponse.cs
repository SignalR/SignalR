using System.Collections.Generic;
using Newtonsoft.Json;

namespace SignalR
{
    /// <summary>
    /// Represents a response to a connection.
    /// </summary>
    public class PersistentResponse
    {
        /// <summary>
        /// The id of the last message in the connection received.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The list of messages to be sent to the receiving connection.
        /// </summary>
        public IList<string> Messages { get; set; }

        /// <summary>
        /// True if the connection receives a disconnect command.
        /// </summary>
        public bool Disconnect { get; set; }

        /// <summary>
        /// True if the connection was forcibly closed. 
        /// </summary>
        public bool Aborted { get; set; }

        /// <summary>
        /// True if the connection timed out.
        /// </summary>
        public bool TimedOut { get; set; }

        /// <summary>
        /// Transport specific configurtion information.
        /// </summary>
        public IDictionary<string, object> TransportData { get; set; }
    }
}