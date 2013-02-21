using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ResponseWriterFacts
    {
        [Fact]
        public void CanEncodingSurrogatePairsCorrectly()
        {
            var bytes = new List<byte>();
            var writer = new ResponseWriter((buffer, state) =>
            {
                for (int i = buffer.Offset; i < buffer.Count; i++)
                {
                    bytes.Add(buffer.Array[i]);
                }
            }, null);

            writer.Write("\U00024B62"[0]);
            writer.Write("\U00024B62"[1]);

            var expected = new byte[] { 0xF0, 0xA4, 0xAD, 0xA2 };

            Assert.Equal(expected, bytes);
        }
    }
}
