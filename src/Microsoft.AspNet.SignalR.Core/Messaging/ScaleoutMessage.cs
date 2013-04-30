using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMessage
    {
        public ScaleoutMessage(IList<Message> messages)
        {
            Messages = messages;
            CreationTime = DateTime.UtcNow;
        }

        public ScaleoutMessage()
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This type is used for serialization")]
        public IList<Message> Messages { get; set; }
        public DateTime CreationTime { get; set; }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                var binaryWriter = new BinaryWriter(ms);

                binaryWriter.Write(Messages.Count);
                for (int i = 0; i < Messages.Count; i++)
                {
                    Messages[i].WriteTo(ms);
                }
                binaryWriter.Write(CreationTime.ToString("s", CultureInfo.InvariantCulture));

                return ms.ToArray();
            }
        }

        public static ScaleoutMessage FromBytes(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            using (var stream = new MemoryStream(data))
            {
                var binaryReader = new BinaryReader(stream);
                var message = new ScaleoutMessage();
                message.Messages = new List<Message>();
                int count = binaryReader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    message.Messages.Add(Message.ReadFrom(stream));
                }

                message.CreationTime = DateTime.Parse(binaryReader.ReadString(), CultureInfo.InvariantCulture);

                return message;
            }
        }
    }
}
