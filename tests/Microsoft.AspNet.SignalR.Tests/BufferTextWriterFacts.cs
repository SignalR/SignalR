using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class BufferTextWriterFacts
    {
        [Fact]
        public void CanEncodingSurrogatePairsCorrectly()
        {
            var bytes = new List<byte>();
            var writer = new BinaryTextWriter((buffer, state) =>
            {
                for (int i = buffer.Offset; i < buffer.Count; i++)
                {
                    bytes.Add(buffer.Array[i]);
                }
            }, null, reuseBuffers: true, bufferSize: 128);

            writer.Write("\U00024B62"[0]);
            writer.Write("\U00024B62"[1]);
            writer.Flush();

            var expected = new byte[] { 0xF0, 0xA4, 0xAD, 0xA2 };

            Assert.Equal(expected, bytes);
        }

        [Fact]
        public void WriteNewBufferIsUsedForWritingChunksIfReuseBuffersFalse()
        {
            var buffers = new List<ArraySegment<byte>>();
            var writer = new BinaryTextWriter((buffer, state) =>
            {
                buffers.Add(buffer);
            },
            null, reuseBuffers: false, bufferSize: 128);

            writer.Write(new string('C', 10000));
            writer.Flush();

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
            var writer = new BinaryTextWriter((buffer, state) =>
            {
                buffers.Add(buffer);
            },
            null, reuseBuffers: true, bufferSize: 128);

            writer.Write(new string('C', 10000));
            writer.Flush();

            Assert.True(buffers.Count > 1);
            var underlyingBuffer = buffers[0].Array;
            for (int i = 1; i < buffers.Count; i++)
            {
                Assert.Same(underlyingBuffer, buffers[i].Array);
            }
        }

        [Fact]
        public void WritesInChunks()
        {
            int bufferSize = 500;
            int size = 3000;

            var buffers = new List<ArraySegment<byte>>();
            var writer = new BinaryTextWriter((buffer, state) =>
            {
                buffers.Add(buffer);
            },
            null, reuseBuffers: true, bufferSize: bufferSize);

            writer.Write(new string('C', size));
            writer.Flush();

            var expected = GetChunks(size, bufferSize).ToArray();

            Assert.NotEmpty(buffers);
            Assert.Equal(expected.Length, buffers.Count);

            for (int i = 0; i < buffers.Count; i++)
            {
                Assert.Equal(expected[i], buffers[i].Count);
            }
        }

        private IEnumerable<int> GetChunks(int size, int bufferSize)
        {
            int num = size / bufferSize;
            int last = size % bufferSize;
            var chunks = Enumerable.Range(0, num)
                             .Select(i => bufferSize);

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }

            if (last != 0)
            {
                yield return last;
            }
        }

        [Fact]
        public void CanInterleaveStringsAndRawBinary()
        {
            var buffers = new List<ArraySegment<byte>>();
            var writer = new BinaryTextWriter((buffer, state) =>
            {
                buffers.Add(buffer);
            },
            null, reuseBuffers: true, bufferSize: 128);

            var encoding = new UTF8Encoding();

            writer.Write('H');
            writer.Write('e');
            writer.Write("llo ");
            writer.Write(new ArraySegment<byte>(encoding.GetBytes("World")));
            writer.Flush();

            Assert.Equal(2, buffers.Count);
            var s = "";
            foreach (var buffer in buffers)
            {
                s += encoding.GetString(buffer.Array, buffer.Offset, buffer.Count);
            }
            Assert.Equal("Hello World", s);
        }
    }
}
