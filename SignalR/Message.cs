using System;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Message
    {
        public static TimeSpan ExpiresAfter
        {
            get;
            set;
        }

        static Message()
        {
            ExpiresAfter = TimeSpan.FromSeconds(30);
        }

        public string SignalKey { get; set; }
        public object Value { get; private set; }
        public ulong Id { get; private set; }
        public DateTime Created { get; private set; }

        public bool Expired
        {
            get
            {
                // TODO: Handle disconnect timeout
                return DateTime.Now.Subtract(Created) >= ExpiresAfter;
            }
        }

        private Message() { }

        public Message(string signalKey, ulong id, object value)
            : this(signalKey, id, value, DateTime.Now)
        {

        }

        public Message(string signalKey, ulong id, object value, DateTime created)
        {
            SignalKey = signalKey;
            Value = value;
            Id = id;
            Created = created;
        }        
    }
}