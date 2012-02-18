using System;

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

        public Message(string signalKey, object value)
            : this(signalKey, value, DateTime.Now)
        {

        }

        public Message(string signalKey, object value, DateTime created)
        {
            SignalKey = signalKey;
            Value = value;
            Created = created;
        }        
    }
}