// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessage
    {
        public long Id { get; private set; }

        public IList<Message> Messages { get; private set; }

        public static byte[] ToBytes(long id, IList<Message> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            using (var ms = new MemoryStream())
            {
                var binaryWriter = new BinaryWriter(ms);

                binaryWriter.Write(id);
                binaryWriter.Write(messages.Count);
                for (int i = 0; i < messages.Count; i++)
                {
                    messages[i].WriteTo(ms);
                }

                return ms.ToArray();
            }
        }

        public static RedisMessage FromBytes(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var binaryReader = new BinaryReader(stream);
                var message = new RedisMessage();
                message.Id = binaryReader.ReadInt64();
                message.Messages = new List<Message>();
                int count = binaryReader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    message.Messages.Add(Message.ReadFrom(stream));
                }

                return message;
            }
        }
    }
}
