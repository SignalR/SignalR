// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.AspNet.SignalR.Messaging
{
    [Serializable]
    public class Message
    {
        private readonly Encoding _encoding;
 
        public Message()
        {
            _encoding = new UTF8Encoding();
        }

        public Message(string source, string key, string value)
            : this(source, key, value, new UTF8Encoding())
        {
        }

        public Message(string source, string key, string value, Encoding encoding)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            Source = source;
            Key = key;
            Value = value == null ? new ArraySegment<byte>() : new ArraySegment<byte>(encoding.GetBytes(value));
            _encoding = encoding;
        }

        public Message(string source, string key, ArraySegment<byte> value)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            Source = source;
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Which connection the message originated from
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The signal for the message (connection id, group, etc)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The message payload
        /// </summary>
        public ArraySegment<byte> Value { get; set; }

        /// <summary>
        /// The command id if this message is a command
        /// </summary>
        public string CommandId { get; set; }

        /// <summary>
        /// Determines if the caller should wait for acknowledgement for this message
        /// </summary>
        public bool WaitForAck { get; set; }

        /// <summary>
        /// Determines if this message is itself an ACK
        /// </summary>
        public bool IsAck { get; set; }

        /// <summary>
        /// A list of connection ids to filter out
        /// </summary>
        public string Filter { get; set; }

        public bool IsCommand
        {
            get
            {
                return CommandId != null;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This may be expensive")]
        public string GetString()
        {
            // If there's no encoding this is a raw binary payload
            if (_encoding == null)
            {
                throw new NotSupportedException();
            }

            return _encoding.GetString(Value.Array, Value.Offset, Value.Count);
        }
    }
}
