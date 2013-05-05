// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessage
    {
        public ulong Id { get; private set; }
        public ScaleoutMessage ScaleoutMessage { get; private set; }

        public static byte[] ToBytes(long id, IList<Message> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            using (var ms = new MemoryStream())
            {
                var binaryWriter = new BinaryWriter(ms);

                var scaleoutMessage = new ScaleoutMessage(messages);
                var buffer = scaleoutMessage.ToBytes();

                binaryWriter.Write(id);
                binaryWriter.Write(buffer.Length);
                binaryWriter.Write(buffer);

                return ms.ToArray();
            }
        }

        public static RedisMessage FromBytes(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var binaryReader = new BinaryReader(stream);
                var message = new RedisMessage();

                message.Id = (ulong)binaryReader.ReadInt64();
                int count = binaryReader.ReadInt32();
                byte[] buffer = binaryReader.ReadBytes(count);

                message.ScaleoutMessage = ScaleoutMessage.FromBytes(buffer);
                return message;
            }
        }
    }
}
