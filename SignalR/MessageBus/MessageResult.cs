using System.Collections.Generic;

namespace SignalR
{
    public struct MessageResult
    {
        private static readonly List<Message> _emptyList = new List<Message>();

        public IList<Message> Messages { get; private set; }
        public string LastMessageId { get; private set; }
        public bool TimedOut { get; set; }

        public MessageResult(string lastMessageId, bool timedOut)
            : this(_emptyList, lastMessageId)
        {
            TimedOut = timedOut;
        }

        public MessageResult(IList<Message> messages, string lastMessageId)
            : this()
        {
            Messages = messages;
            LastMessageId = lastMessageId;
        }
    }
}
