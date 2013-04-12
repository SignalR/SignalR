// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;

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

            var ms = new MemoryStream();
            var binaryWriter = new BinaryWriter(ms);
            binaryWriter.Write(messages.Count);
            for (int i = 0; i < messages.Count; i++)
            {
                messages[i].WriteTo(ms);
            }

            return ms;
        }

        public static IList<Message> FromStream(Stream stream)
        {
            var binaryReader = new BinaryReader(stream);
            int count = binaryReader.ReadInt32();

            var messages = new List<Message>();
            for (int i = 0; i < count; i++)
            {
                messages.Add(Message.ReadFrom(stream));
            }

            return messages;
        }
    }
}
