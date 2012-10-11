using System.Text;
using Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class ChunkBufferFacts
    {
        public class ReadLine
        {
            [Fact]
            public void ReturnsNullIfNoNewLineIfBuffer()
            {
                // Arrange
                var buffer = new ChunkBuffer();
                byte[] data = Encoding.UTF8.GetBytes("hello world");

                // Act
                buffer.Add(data, data.Length);

                // Assert
                Assert.Null(buffer.ReadLine());
            }

            [Fact]
            public void ReturnsTextUpToNewLine()
            {
                // Arrange
                var buffer = new ChunkBuffer();
                byte[] data = Encoding.UTF8.GetBytes("hello world\noy");

                // Act
                buffer.Add(data, data.Length);

                // Assert
                Assert.Equal("hello world", buffer.ReadLine());
            }

            [Fact]
            public void CanReadMultipleLines()
            {
                // Arrange
                var buffer = new ChunkBuffer();
                byte[] data = Encoding.UTF8.GetBytes("hel\nlo world\noy");

                // Act
                buffer.Add(data, data.Length);

                // Assert
                Assert.Equal("hel", buffer.ReadLine());
                Assert.Equal("lo world", buffer.ReadLine());
                Assert.Null(buffer.ReadLine());
            }

            [Fact]
            public void WillCompleteNewLine()
            {
                // Arrange
                var buffer = new ChunkBuffer();
                byte[] data = Encoding.UTF8.GetBytes("hello");
                buffer.Add(data, data.Length);
                Assert.Null(buffer.ReadLine());
                data = Encoding.UTF8.GetBytes("\n");
                buffer.Add(data, data.Length);
                Assert.Equal("hello", buffer.ReadLine());
                data = Encoding.UTF8.GetBytes("Another line");
                buffer.Add(data, data.Length);
                Assert.Null(buffer.ReadLine());
                data = Encoding.UTF8.GetBytes("\nnext");
                buffer.Add(data, data.Length);
                Assert.Equal("Another line", buffer.ReadLine());
            }
        }
    }
}
