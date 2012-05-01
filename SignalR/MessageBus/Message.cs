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
        private DateTime ExpiresAt { get; set; }

        public bool Expired
        {
            get
            {
                return DateTime.UtcNow >= ExpiresAt;
            }
        }

        private Message() { }

        public Message(string signalKey, object value)
            : this(signalKey, value, DateTime.UtcNow)
        {

        }

        public Message(string signalKey, object value, DateTime created)
        {
            SignalKey = signalKey;
            Value = value;
            Created = created;
            ExpiresAt = created.Add(ExpiresAfter);
        }
    }
}