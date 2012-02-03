using System.Collections.Generic;

namespace SignalR.MessageBus
{
    public struct MessageResult
    {
        public IList<Message> Messages { get; private set; }
        public string LastMessageId { get; private set; }

        public MessageResult(IList<Message> messages, string lastMessageId)
            : this()
        {
            Messages = messages;
            LastMessageId = lastMessageId;
        }
    }
}
