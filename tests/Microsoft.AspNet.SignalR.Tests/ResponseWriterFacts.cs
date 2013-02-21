using System;
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
            }, null, reuseBuffers: true);

            writer.Write("\U00024B62"[0]);
            writer.Write("\U00024B62"[1]);

            var expected = new byte[] { 0xF0, 0xA4, 0xAD, 0xA2 };

            Assert.Equal(expected, bytes);
        }

        [Fact]
        public void WriteNewBufferIsUsedForWritingChunksIfReuseBuffersFalse()
        {
            var buffers = new List<ArraySegment<byte>>();
            var writer = new ResponseWriter((buffer, state) =>
            {
                buffers.Add(buffer);
            },
            null, reuseBuffers: false);

            writer.Write(new string('C', 10000));

            Assert.True(buffers.Count > 1);
            var underlyingBuffer = buffers[0].Array;
            for (int i = 1; i < buffers.Count; i++)
            {
                Assert.NotSame(underlyingBuffer, buffers[i].Array);
            }
        }

        [Fact]
        public void WriteSameBufferIsUsedForWritingChunksIfReuseBuffersTrue()
        {
            var buffers = new List<ArraySegment<byte>>();
            var writer = new ResponseWriter((buffer, state) =>
            {
                buffers.Add(buffer);
            },
            null, reuseBuffers: true);

            writer.Write(new string('C', 10000));

            Assert.True(buffers.Count > 1);
            var underlyingBuffer = buffers[0].Array;
            for (int i = 1; i < buffers.Count; i++)
            {
                Assert.Same(underlyingBuffer, buffers[i].Array);
            }
        }
    }
}
