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

            // Act
            response.Object.End("Hello World");

            // Assert
            Assert.Equal("Hello World", value);
        }
    }
}
