// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public static class ServiceBusMessage
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

        public static ScaleoutMessage FromBrokeredMessage(BrokeredMessage brokeredMessage)
        {
            if (brokeredMessage == null)
            {
                throw new ArgumentNullException("brokeredMessage");
            }

            var stream = brokeredMessage.GetBody<Stream>();

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);

                var message = ScaleoutMessage.FromBytes(ms.ToArray());

                return message;
            }
        }
    }
}
