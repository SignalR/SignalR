using System;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNet.SignalR.WebSockets;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Transports
{
    public class WebSocketTransportFacts
    {
        [Fact]
        public void MessageChunksAreSentCorrectly()
        {
            // Disable DefaultWebSocketHandler's maxIncomingMessageSize
            var webSocketHandler = new Mock<DefaultWebSocketHandler>(null);
            webSocketHandler.CallBase = true;

            bool sendAsyncMethodExecuted = false;
            var arraySegment = new ArraySegment<byte>();
            bool endOfMessage = false;

            webSocketHandler.Setup(w => w.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<WebSocketMessageType>(), It.IsAny<bool>()))
                .Callback<ArraySegment<byte>, WebSocketMessageType, bool>((arraySegmentValue, messageTypeValue, endOfMessageValue) =>
                {
                    sendAsyncMethodExecuted = true;
                    arraySegment = arraySegmentValue;
                    endOfMessage = endOfMessageValue;
                });

            webSocketHandler.Object.SendChunk(new ArraySegment<byte>(new byte[1] { (byte)'a' })).Wait();

            Assert.False(sendAsyncMethodExecuted);
            Assert.False(endOfMessage);

            webSocketHandler.Object.Flush();

            Assert.True(sendAsyncMethodExecuted);
            Assert.Equal(arraySegment.Array[0], (byte)'a');
            Assert.True(endOfMessage);

            Assert.Equal(0, webSocketHandler.Object.NextMessageToSend.Count);
        }
    }
}
