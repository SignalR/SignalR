// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Json
{
    /// <summary>
    /// Extensions for <see cref="JsonSerializer"/>.
    /// </summary>
    public static class JsonSerializerExtensions
    {
        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <param name="serializer">The serializer</param>
        /// <typeparam name="T">The <see cref="System.Type"/> of object being deserialized.</typeparam>
        /// <param name="json">The JSON to deserialize</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public static T Parse<T>(this JsonSerializer serializer, string json)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            using (var reader = new StringReader(json))
            {
                return (T)serializer.Deserialize(reader, typeof(T));
            }
        }

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <param name="serializer">The serializer</param>
        /// <typeparam name="T">The <see cref="System.Type"/> of object being deserialized.</typeparam>
        /// <param name="jsonBuffer">The JSON buffer to deserialize</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        public static T Parse<T>(this JsonSerializer serializer, ArraySegment<byte> jsonBuffer, Encoding encoding)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            using (var reader = new ArraySegmentTextReader(jsonBuffer, encoding))
            {
                return (T)serializer.Deserialize(reader, typeof(T));
            }
        }

        /// <summary>
        /// Serializes the specified object to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="serializer">The JSON serializer</param>
        /// <param name="value">The object to serialize</param>
        /// <param name="writer">The <see cref="TextWriter"/> to serialize the object to.</param>
        public static void Serialize(this JsonSerializer serializer, object value, TextWriter writer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            var selfSerializer = value as IJsonWritable;

            if (selfSerializer != null)
            {
                selfSerializer.WriteJson(writer);
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }

        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="serializer">The serializer</param>
        /// <param name="value">The object to serailize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string Stringify(this JsonSerializer serializer, object value)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                serializer.Serialize(value, writer);
                return writer.ToString();
            }
        }
    }
}
