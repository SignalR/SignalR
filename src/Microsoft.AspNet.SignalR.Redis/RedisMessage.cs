// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Redis
{
    public class RedisMessage
    {
        public ulong Id { get; private set; }
        public ScaleoutMessage ScaleoutMessage { get; private set; }

        public static byte[] ToBytes(IList<Message> messages)
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

                binaryWriter.Write(buffer.Length);
                binaryWriter.Write(buffer);

                return ms.ToArray();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static RedisMessage FromBytes(byte[] data, TraceSource trace)
        {
            using (var stream = new MemoryStream(data))
            {
                var message = new RedisMessage();

                // read message id from memory stream until SPACE character
                var messageIdBuilder = new StringBuilder(20);
                do
                {
                    // it is safe to read digits as bytes because they encoded by single byte in UTF-8
                    int charCode = stream.ReadByte();
                    if (charCode == -1)
                    {
                        trace.TraceVerbose("Received Message could not be parsed.");
                        throw new EndOfStreamException(Resources.Error_EndOfStreamRedis);
                    }

                    char c = (char)charCode;

                    if (c == ' ')
                    {
                        message.Id = ulong.Parse(messageIdBuilder.ToString(), CultureInfo.InvariantCulture);
                        messageIdBuilder = null;
                    }
                    else
                    {
                        messageIdBuilder.Append(c);
                    }
                }
                while (messageIdBuilder != null);

                var binaryReader = new BinaryReader(stream);
                int count = binaryReader.ReadInt32();
                byte[] buffer = binaryReader.ReadBytes(count);

                message.ScaleoutMessage = ScaleoutMessage.FromBytes(buffer);
                return message;
            }
        }
    }
}
