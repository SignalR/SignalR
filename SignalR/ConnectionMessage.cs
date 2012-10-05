using System.Collections.Generic;

namespace SignalR
{
    public struct ConnectionMessage
    {
        public string Signal { get; set; }
        public object Value { get; set; }
        public IEnumerable<string> ExcludedSignals { get; set; }

        public ConnectionMessage(string signal, object value)
            : this()
        {
            Signal = signal;
            Value = value;
        }
    }
}
