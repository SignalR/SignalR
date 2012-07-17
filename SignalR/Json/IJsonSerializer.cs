using System;

namespace SignalR
{
    /// <summary>
    /// Used to serialize and deserialze outgoing/incoming data.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serailize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        string Stringify(object value);

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <param name="json">The JSON to deserialize.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        object Parse(string json);

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <param name="json">The JSON to deserialize.</param>
        /// <param name="targetType">The <see cref="System.Type"/> of object being deserialized.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        object Parse(string json, Type targetType);

        /// <summary>
        /// Deserializes the JSON to a .NET object.
        /// </summary>
        /// <typeparam name="T">The <see cref="System.Type"/> of object being deserialized.</typeparam>
        /// <param name="json">The JSON to deserialize</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        T Parse<T>(string json);
    }
}