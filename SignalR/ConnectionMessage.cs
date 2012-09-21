namespace SignalR
{
    public struct ConnectionMessage
    {
        public string Signal { get; private set; }
        public object Value { get; private set; }
        public bool IgnoreSender { get; private set; }

        public ConnectionMessage(string signal, object value, bool ignoreSender)
            : this()
        {
            Signal = signal;
            Value = value;
            IgnoreSender = ignoreSender;
        }
    }
}
