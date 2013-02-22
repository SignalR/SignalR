using System;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// Implemented on anything that has the ability to write raw binary data
    /// </summary>
    public interface IBinaryWriter
    {
        void Write(ArraySegment<byte> data);
    }
}
