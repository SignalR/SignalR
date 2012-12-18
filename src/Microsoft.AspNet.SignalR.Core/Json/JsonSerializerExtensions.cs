﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Extensions for <see cref="IJsonSerializer"/>.
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
        public static T Parse<T>(this IJsonSerializer serializer, string json)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            return (T)serializer.Parse(json, typeof(T));
        }

        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="serializer">The serializer</param>
        /// <param name="value">The object to serailize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string Stringify(this IJsonSerializer serializer, object value)
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
