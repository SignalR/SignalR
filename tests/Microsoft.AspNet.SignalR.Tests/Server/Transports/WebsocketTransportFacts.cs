using System;
using System.Net.WebSockets;
using Microsoft.AspNet.SignalR.WebSockets;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Transports
{
    public class WebSocketTransportFacts
    {
        [Fact]
        public void SendChunkDoesNotSendChunksEagerly()
        {
            // Disable DefaultWebSocketHandler's maxIncomingMessageSize
            var mockWebSocketHandler = new Mock<DefaultWebSocketHandler>(null)
            {
                CallBase = true
            };

            var webSocketHandler = mockWebSocketHandler.Object;
            webSocketHandler.SendChunk(new ArraySegment<byte>(new[] { (byte)'a' })).Wait();

            Assert.Equal(1, webSocketHandler.NextMessageToSend.Count);
            mockWebSocketHandler.Verify(
                p => p.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>()), 
                Times.Never());
        }

        [Fact]
        public void FlushSendsPendingChunks()
        {
            var message = new[] { (byte)'a' };

            // Disable DefaultWebSocketHandler's maxIncomingMessageSize
            var mockWebSocketHandler = new Mock<DefaultWebSocketHandler>(null);

            var webSocketHandler = mockWebSocketHandler.Object;
            webSocketHandler.SendChunk(new ArraySegment<byte>(message)).Wait();
            webSocketHandler.Flush();

            mockWebSocketHandler.Verify(p => p.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true), Times.Once());
            Assert.Equal(0, webSocketHandler.NextMessageToSend.Count);
        }
    }
}
