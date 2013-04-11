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

        [Fact]
        public void ThrowingErrorOnCloseRaisesOnClosed()
        {
            var webSocketHandler = new Mock<WebSocketHandler>();
            var webSocket = new Mock<WebSocket>();
            var cts = new CancellationTokenSource(); 
            webSocketHandler.Setup(wsh => wsh.Close()).Throws(new Exception("It's disconnected"));

            cts.Cancel();
            webSocketHandler.Object.ProcessWebSocketRequestAsync(webSocket.Object, cts.Token);

            webSocketHandler.Verify(wsh => wsh.OnClose(It.IsAny<bool>()), Times.AtLeastOnce());
        }

        [Fact]
        public void WebSocketHandlesClosedMessageGracefully()
        {
            var messageIndex = 0;
            var webSocketMessages = new[] { new WebSocketReceiveResult(0, WebSocketMessageType.Text, endOfMessage: false),
                                            new WebSocketReceiveResult(0, WebSocketMessageType.Text, endOfMessage: false),
                                            new WebSocketReceiveResult(0, WebSocketMessageType.Close, endOfMessage: true)};

            var webSocket = new Mock<WebSocket>(MockBehavior.Strict);
            var webSocketHandler = new Mock<WebSocketHandler>(MockBehavior.Strict);

            webSocket.Setup(w => w.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), CancellationToken.None))
                     .Returns(() => TaskAsyncHelper.FromResult(webSocketMessages[messageIndex++]));

            webSocketHandler.Setup(h => h.OnOpen());
            webSocketHandler.Setup(h => h.OnClose(true));
            webSocketHandler.Setup(h => h.Close());

            webSocketHandler.Object.ProcessWebSocketRequestAsync(webSocket.Object, CancellationToken.None).Wait();

            webSocket.VerifyAll();
            webSocketHandler.VerifyAll();
        }
    }
}
