using System;

namespace SignalR
{
    public class Message
    {
        public string Key { get; private set; }
        public object Value { get; private set; } 
        
        public Message(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}