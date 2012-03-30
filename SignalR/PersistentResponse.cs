using System.Collections.Generic;

namespace SignalR
{
    public class PersistentResponse
    {
        private readonly IDictionary<string, object> _transportData = new Dictionary<string, object>();

        public string MessageId { get; set; }
        public IEnumerable<object> Messages { get; set; }
        public bool Disconnect { get; set; }
        public bool TimedOut { get; set; }

        // TODO: Don't seralize TransportData to the response if there is none
        public IDictionary<string, object> TransportData
        {
            get { return _transportData; }
        }
    }
}