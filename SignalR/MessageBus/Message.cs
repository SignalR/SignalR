using System;

namespace SignalR
{
    public class Message
    {
        public string Key { get; private set; }
        public string Value { get; private set; } 
        
        public Message(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}