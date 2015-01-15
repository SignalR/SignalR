using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class MemoryPoolTextWriterFacts
    {
        [Fact]
        public void CanEncodingSurrogatePairsCorrectly()
        {
            using(var writer = new MemoryPoolTextWriter(new MemoryPool()))
            {
                writer.Write("\U00024B62"[0]);
                writer.Write("\U00024B62"[1]);
                writer.Flush();

                var expected = new byte[] { 0xF0, 0xA4, 0xAD, 0xA2 };
                Assert.Equal(expected, writer.Buffer);
            }
        }

        [Fact]
        public void CanInterleaveStringsAndRawBinary()
        {
            var buffers = new List<ArraySegment<byte>>();
            using (var writer = new MemoryPoolTextWriter(new MemoryPool()))
            {
                var encoding = new UTF8Encoding();
                writer.Write('H');
                writer.Write('e');
                writer.Write("llo ");
                writer.Write(new ArraySegment<byte>(encoding.GetBytes("World")));
                writer.Flush();

                Assert.Equal("Hello World", encoding.GetString(writer.Buffer.ToArray()));
            }
        }

        [Fact]
        public void StringOverrideBehavesAsCharArray()
        {
            var writer = new MemoryPoolTextWriter(new MemoryPool());
            var testTxt = new string('m', 260);

            writer.Write(testTxt.ToCharArray(), 0, testTxt.Length);
            writer.Flush();

            var encoding = new UTF8Encoding();
            Assert.Equal(testTxt, encoding.GetString(writer.Buffer.ToArray()));
        }
    }
}
