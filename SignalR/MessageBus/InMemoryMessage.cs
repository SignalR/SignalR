using System;

namespace SignalR
{
    public class InMemoryMessage : Message
    {        
        public InMemoryMessage(string key, object value)
            : base(key, value)
        {

        }
    }

    public class InMemoryMessage<T> : Message where T : IComparable<T>
    {
        public T Id { get; private set; }

        public InMemoryMessage(string key, object value, T id)
            : base(key, value)
        {
            Id = id;
        }
    }
}
