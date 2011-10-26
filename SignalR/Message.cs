using System;

namespace SignalR
{
    public class Message
    {
        public string SignalKey { get; set; }
        public object Value { get; private set; }
        public long Id { get; private set; }
        public DateTime Created { get; private set; }

        public bool Expired
        {
            get
            {
                return DateTime.Now.Subtract(Created).TotalSeconds >= 30;
            }
        }

        private Message() { }

        public Message(string signalKey, long id, object value)
            : this(signalKey, id, value, DateTime.Now)
        {

        }

        public Message(string signalKey, long id, object value, DateTime created)
        {
            SignalKey = signalKey;
            Value = value;
            Id = id;
            Created = created;
        }
    }
}