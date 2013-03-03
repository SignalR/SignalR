// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public class SqlPayload
    {
        public IList<Message> Messages { get; private set; }

        public static byte[] ToBytes(IList<Message> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            using (var ms = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(ms))
            {   
                binaryWriter.Write(messages.Count);
                
                for (int i = 0; i < messages.Count; i++)
                {
                    messages[i].WriteTo(ms);
                }

                return ms.ToArray();
            }
        }

        public static SqlPayload FromBytes(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var binaryReader = new BinaryReader(stream))
            {
                int count = binaryReader.ReadInt32();

                var payload = new SqlPayload { Messages = new List<Message>() };
                for (int i = 0; i < count; i++)
                {
                    payload.Messages.Add(Message.ReadFrom(stream));
                }

                return payload;
            }
        }
    }
}
