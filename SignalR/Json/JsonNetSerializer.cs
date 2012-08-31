using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SignalR
{
    /// <summary>
    /// Default <see cref="IJsonSerializer"/> implementation over Json.NET.
    /// </summary>
    public class JsonNetSerializer : IJsonSerializer
    {
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetSerializer"/> class.
        /// </summary>
        public JsonNetSerializer()
            : this(new JsonSerializerSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetSerializer"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="T:Newtonsoft.Json.JsonSerializerSettings"/> to use when serializing and deserializing.</param>
        public JsonNetSerializer(JsonSerializerSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            _serializer = JsonSerializer.Create(settings);
        }
        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serailize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public string Stringify(object value)
        {
            using (var writer = new StringWriter())
            {
                _serializer.Serialize(writer, value);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <param name="json">The JSON to deserialize.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public object Parse(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var reader = new JsonTextReader(stringReader))
            {
                return _serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <param name="json">The JSON to deserialize.</param>
        /// <param name="targetType">The <see cref="System.Type"/> of object being deserialized.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public object Parse(string json, Type targetType)
        {
            using (var reader = new StringReader(json))
            {
                return _serializer.Deserialize(reader, targetType);
            }
        }

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <typeparam name="T">The <see cref="System.Type"/> of object being deserialized.</typeparam>
        /// <param name="json">The JSON to deserialize</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public T Parse<T>(string json)
        {
            return (T)Parse(json, typeof(T));
        }

        public void Stringify(object value, TextWriter writer)
        {
            // REVIEW: This is a hack to improve performance, we need to abstract this
            // json writer so we can do it generically (but that might not be worth it).
            var response = value as PersistentResponse;
            if (response != null)
            {
                SerializePesistentResponse(response, writer);
            }
            else
            {
                _serializer.Serialize(writer, value);
            }
        }

        private void SerializePesistentResponse(PersistentResponse response, TextWriter writer)
        {
            var jsonWriter = new JsonTextWriter(writer);
            jsonWriter.WriteStartObject();

            jsonWriter.WritePropertyName("MessageId");
            jsonWriter.WriteValue(response.MessageId);

            jsonWriter.WritePropertyName("Disconnect");
            jsonWriter.WriteValue(response.Disconnect);

            jsonWriter.WritePropertyName("TimedOut");
            jsonWriter.WriteValue(response.TimedOut);

            if (response.TransportData != null)
            {
                jsonWriter.WritePropertyName("TransportData");
                jsonWriter.WriteStartObject();

                object value;
                if (response.TransportData.TryGetValue("Groups", out value))
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

            for (int i = 0; i < response.Messages.Count; i++)
            {
                for (int j = response.Messages[i].Offset; j < response.Messages[i].Offset + response.Messages[i].Count; j++)
                {
                    Message message = response.Messages[i].Array[j];
                    if (!SignalCommand.IsCommand(message))
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