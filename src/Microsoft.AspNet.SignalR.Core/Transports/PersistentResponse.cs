// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Transports
{
    /// <summary>
    /// Represents a response to a connection.
    /// </summary>
    public sealed class PersistentResponse : IJsonWritable
    {
        /// <summary>
        /// Creates a new instance of <see cref="PersistentResponse"/>.
        /// </summary>
        public PersistentResponse()
            : this(m => false)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="PersistentResponse"/>.
        /// </summary>
        /// <param name="exclude">A filter that determines whether messages should be written to the client.</param>
        public PersistentResponse(Func<Message, bool> exclude)
        {
            ExcludeFilter = exclude;
        }

        /// <summary>
        /// A filter that determines whether messages should be written to the client.
        /// </summary>
        public Func<Message, bool> ExcludeFilter { get; private set; }

        /// <summary>
        /// The id of the last message in the connection received.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The list of messages to be sent to the receiving connection.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an optimization and this type is only used for serialization.")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This type is only used for serialization")]
        public IList<ArraySegment<Message>> Messages { get; set; }

        /// <summary>
        /// The total count of the messages sent the receiving connection.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// True if the connection receives a disconnect command.
        /// </summary>
        public bool Disconnect { get; set; }

        /// <summary>
        /// True if the connection was forcibly closed. 
        /// </summary>
        public bool Aborted { get; set; }

        /// <summary>
        /// True if the connection timed out.
        /// </summary>
        public bool TimedOut { get; set; }

        /// <summary>
        /// Signed token representing the list of groups. Updates on change.
        /// </summary>
        public string GroupsToken { get; set; }

        /// <summary>
        /// The time the long polling client should wait before reestablishing a connection if no data is received.
        /// </summary>
        public long? LongPollDelay { get; set; }

        /// <summary>
        /// Serializes only the necessary components of the <see cref="PersistentResponse"/> to JSON
        /// using Json.NET's JsonTextWriter to improve performance.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> that receives the JSON serialization.</param>
        void IJsonWritable.WriteJson(TextWriter writer)
        {
            var jsonWriter = new JsonTextWriter(writer);
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("C");
            jsonWriter.WriteValue(MessageId);

            if (Disconnect)
            {
                jsonWriter.WritePropertyName("D");
                jsonWriter.WriteValue(1);
            }

            if (TimedOut)
            {
                jsonWriter.WritePropertyName("T");
                jsonWriter.WriteValue(1);
            }
            
            if (GroupsToken != null)
            {
                jsonWriter.WritePropertyName("G");
                jsonWriter.WriteValue(GroupsToken);
            }

            if (LongPollDelay.HasValue)
            {
                jsonWriter.WritePropertyName("L");
                jsonWriter.WriteValue(LongPollDelay.Value);
            }

            jsonWriter.WritePropertyName("M");
            jsonWriter.WriteStartArray();

            Messages.Enumerate(m => !m.IsCommand && !ExcludeFilter(m),
                               (w, m) => w.WriteRawValue(m.Value),
                               jsonWriter);

            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }
    }
}
