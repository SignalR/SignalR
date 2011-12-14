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

        private readonly Lazy<SignalCommand> _command;

        public string SignalKey { get; set; }
        public object Value { get; private set; }
        public long Id { get; private set; }
        public DateTime Created { get; private set; }

        public bool Expired
        {
            get
            {
                var expiresAfter = ExpiresAfter;
                if (_command.Value != null && _command.Value.ExpiresAfter.HasValue)
                {
                    expiresAfter = _command.Value.ExpiresAfter.Value;
                }

                return DateTime.Now.Subtract(Created) >= expiresAfter;
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

            _command = new Lazy<SignalCommand>(() =>
            {
                if (!signalKey.EndsWith(SignalCommand.SignalrCommand))
                {
                    return null;
                }

                var command = Value as SignalCommand;

                // Optimization for in memory message store
                if (command != null)
                {
                    return command;
                }

                // Otherwise deserialize the message value
                string rawValue = Value as string;
                if (rawValue == null)
                {
                    return null;
                }

                return DependencyResolver.Resolve<IJsonSerializer>().Parse<SignalCommand>(rawValue);
            });
        }

        internal SignalCommand GetCommand()
        {
            return _command.Value;
        }
    }
}