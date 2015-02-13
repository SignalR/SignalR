using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.EventHub
{
    public static class EventHubMessage
    {
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The stream is returned to the caller of ths method")]
        public static Stream ToStream(IList<Message> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            var scaleoutMessage = new ScaleoutMessage(messages);
            return new MemoryStream(scaleoutMessage.ToBytes());
        }

        public static ScaleoutMessage FromEventData(EventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException("eventData");
            }

            var stream = eventData.GetBody<Stream>();

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                var message = ScaleoutMessage.FromBytes(ms.ToArray());
                return message;
            }
        }

     
    }
}
