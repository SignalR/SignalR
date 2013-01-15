// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.SignalR.Json
{
    /// <summary>
    /// Used to serialize and deserialize outgoing/incoming data.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes the specified object to a <see cref="System.IO.TextWriter"/>.
        /// </summary>
        /// <param name="value">The object to serialize</param>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> to serialize the object to.</param>
        void Serialize(object value, TextWriter writer);

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <param name="json">The JSON to deserialize.</param>
        /// <param name="targetType">The <see cref="System.Type"/> of object being deserialized.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        object Parse(string json, Type targetType);
    }
}
