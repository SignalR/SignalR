using System;
using System.IO;
using System.Text;
using Moq;
using Xunit;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.Tests.Hosting
{
    public class ResponseExtensionsFacts
    {
        [Fact]
        public void WrapperStreamOnlyImplementsWrite()
        {
            // Arrange
            var response = new Mock<IResponse>();
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>())).Verifiable();
            Stream stream = response.Object.AsStream();
            var buffer = Encoding.UTF8.GetBytes("Hello");

            // Act
            stream.Write(buffer, 0, buffer.Length);

            // Assert
            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanTimeout);
            Assert.True(stream.CanWrite);
            response.VerifyAll();
        }

        [Fact]
        public void EndAsyncWritesUtf8BufferToResponse()
        {
            // Arrange
            var response = new Mock<IResponse>();
            string value = null;
            response.Setup(m => m.Write(It.IsAny<ArraySegment<byte>>()))
                    .Callback<ArraySegment<byte>>(data =>
                    {
                        value = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
                    });

            response.Setup(m => m.End()).Verifiable();

            // Act
            response.Object.End("Hello World");

            // Assert
            Assert.Equal("Hello World", value);
            response.VerifyAll();
        }
    }
}
