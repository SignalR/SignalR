using System.IO;

namespace SignalR
{
    /// <summary>
    /// Implementations handle their own serialization to JSON.
    /// </summary>
    public interface IJsonWritable
    {
        /// <summary>
        /// Serializes itself to JSON via a <see cref="System.IO.TextWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> that receives the JSON serialized object.</param>
        void WriteJson(TextWriter writer);
    }
}
