using Microsoft.AspNet.SignalR.WebSockets;
using Moq;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Owin
{
    public class WebSocketFacts
    {
        [Fact]
        public void WebSocketHandlerThrowsCorrectly()
        {
            var webSocketHandler = new Mock<WebSocketHandler>();
            var webSocket = new Mock<WebSocket>();

            webSocketHandler.Setup(wsh => wsh.OnClose(It.IsAny<bool>())).Callback((bool clean) =>
            {
                Assert.False(clean);
            });

            webSocketHandler.Setup(wsh => wsh.OnError()).Callback(() =>
            {
                Assert.IsType<OperationCanceledException>(webSocketHandler.Object.Error);
            });
            
            webSocket.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());
            webSocket.Setup(ws => ws.State).Returns(WebSocketState.Aborted);

            webSocketHandler.Object.ProcessWebSocketRequestAsync(webSocket.Object, CancellationToken.None);

            webSocketHandler.Verify(wsh => wsh.OnError(), Times.AtLeastOnce());
            webSocketHandler.Verify(wsh => wsh.OnClose(It.IsAny<bool>()), Times.AtLeastOnce());
        }
    }
}
