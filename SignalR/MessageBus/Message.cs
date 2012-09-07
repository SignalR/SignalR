namespace SignalR
{
    public class Message
    {
        public string Source { get; private set; }
        public string Key { get; private set; }
        public string Value { get; private set; }
        public bool IsCommand { get; set; }
        
        public Message(string source, string key, string value)
        {
            Source = source;
            Key = key;
            Value = value;
        }
    }
}