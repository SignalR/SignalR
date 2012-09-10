﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SignalR
{
    /// <summary>
    /// Represents a response to a connection.
    /// </summary>
    public class PersistentResponse : IJsonWritable
    {
        /// <summary>
        /// The id of the last message in the connection received.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The list of messages to be sent to the receiving connection.
        /// </summary>
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
        /// Transport specific configurtion information.
        /// </summary>
        public IDictionary<string, object> TransportData { get; set; }

        /// <summary>
        /// Serializes only the necessary components of the <see cref="SignalR.PersistentResponse"/> to JSON
        /// using Json.NET's JsonTextWriter to improve performance.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> that receives the JSON serialization.</param>
        void IJsonWritable.WriteJson(TextWriter writer)
        {
            var jsonWriter = new JsonTextWriter(writer);
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("MessageId");
            jsonWriter.WriteValue(MessageId);

            jsonWriter.WritePropertyName("Disconnect");
            jsonWriter.WriteValue(Disconnect);

            jsonWriter.WritePropertyName("TimedOut");
            jsonWriter.WriteValue(TimedOut);

            if (TransportData != null)
            {
                jsonWriter.WritePropertyName("TransportData");
                jsonWriter.WriteStartObject();

                object value;
                if (TransportData.TryGetValue("Groups", out value))
                {
                    jsonWriter.WritePropertyName("Groups");
                    jsonWriter.WriteStartArray();
                    foreach (var group in (IEnumerable<string>)value)
                    {
                        jsonWriter.WriteValue(group);
                    }
                    jsonWriter.WriteEndArray();
                }

                jsonWriter.WriteEndObject();
            }
            
            jsonWriter.WritePropertyName("Messages");
            jsonWriter.WriteStartArray();

            for (int i = 0; i < Messages.Count; i++)
            {
                ArraySegment<Message> segment = Messages[i];
                for (int j = segment.Offset; j < segment.Offset + segment.Count; j++)
                {
                    Message message = segment.Array[j];
                    if (!message.IsCommand)
                    {
                        jsonWriter.WriteRawValue(message.Value);
                    }
                }
            }

            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }
    }
}