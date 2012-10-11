namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.ServiceBus.Messaging;

    static class MessageConverter
    {
        public static BrokeredMessage ToBrokeredMessages(IEnumerable<Message> messages)
        {
            Stream bodyStream = FastMessageSerializer.GetStream(messages);
            return new BrokeredMessage(bodyStream, true);
        }

        public static Message[] ToMessages(BrokeredMessage brokeredMessage)
        {
            Stream bodyStream = brokeredMessage.GetBody<Stream>();
            return FastMessageSerializer.GetMessages(bodyStream);
        }
    }
}
