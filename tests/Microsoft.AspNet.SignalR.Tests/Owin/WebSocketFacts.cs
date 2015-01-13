using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.WebSockets;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Owin
{
    public class WebSocketFacts
    {
        [Fact]
        public void WebSocketHandlerThrowsCorrectly()
        {
            var webSocketHandler = new Mock<WebSocketHandler>(64 * 1024);
            var webSocket = new Mock<WebSocket>();

            webSocketHandler.Setup(wsh => wsh.OnError()).Callback(() =>
            {
                Assert.IsType<OperationCanceledException>(webSocketHandler.Object.Error);
            });

            webSocket.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());
            webSocket.Setup(ws => ws.State).Returns(WebSocketState.Aborted);

            webSocketHandler.Object.ProcessWebSocketRequestAsync(webSocket.Object, CancellationToken.None);

            webSocketHandler.Verify(wsh => wsh.OnError(), Times.AtLeastOnce());
            webSocketHandler.Verify(wsh => wsh.OnClose(), Times.AtLeastOnce());
        }

        [Fact]
        public void ThrowingErrorOnCloseRaisesOnClosed()
        {
            var webSocketHandler = new Mock<WebSocketHandler>(64 * 1024);
            var webSocket = new Mock<WebSocket>();
            var cts = new CancellationTokenSource();
            webSocketHandler.Setup(wsh => wsh.CloseAsync()).Throws(new Exception("It's disconnected"));

            cts.Cancel();
            webSocketHandler.Object.ProcessWebSocketRequestAsync(webSocket.Object, cts.Token);

            webSocketHandler.Verify(wsh => wsh.OnClose(), Times.AtLeastOnce());
        }

        [Fact]
        public void WebSocketHandlesClosedMessageGracefully()
        {
            var messageIndex = 0;
            var webSocketMessages = new[] { new WebSocketReceiveResult(0, WebSocketMessageType.Text, endOfMessage: false),
                                            new WebSocketReceiveResult(0, WebSocketMessageType.Text, endOfMessage: false),
                                            new WebSocketReceiveResult(0, WebSocketMessageType.Close, endOfMessage: true)};

            var webSocket = new Mock<WebSocket>(MockBehavior.Strict);
            var webSocketHandler = new Mock<WebSocketHandler>(MockBehavior.Strict, 64 * 1024);

            webSocket.Setup(w => w.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), CancellationToken.None))
                     .Returns(() => TaskAsyncHelper.FromResult(webSocketMessages[messageIndex++]));

            webSocketHandler.Setup(h => h.OnOpen());
            webSocketHandler.Setup(h => h.OnClose());
            webSocketHandler.Setup(h => h.CloseAsync()).Returns(TaskAsyncHelper.Empty).Verifiable();

            webSocketHandler.Object.ProcessWebSocketRequestAsync(webSocket.Object, CancellationToken.None).Wait();

            webSocket.VerifyAll();
            webSocketHandler.VerifyAll();
        }

        [Theory]
        [InlineData(WebSocketState.Closed)]
        [InlineData(WebSocketState.CloseSent)]
        [InlineData(WebSocketState.Aborted)]
        public async Task CloseNoopsIfInTerminalState(WebSocketState state)
        {
            var webSocket = new Mock<WebSocket>();
            var webSocketHandler = new Mock<WebSocketHandler>(64 * 1024) {CallBase = true};

            webSocket.Setup(m => m.State).Returns(state);
            webSocketHandler.Object.WebSocket = webSocket.Object;

            await webSocketHandler.Object.CloseAsync();

            webSocket.Verify(m => m.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None), Times.Never());
        }

        [Theory]
        [InlineData(WebSocketState.Closed)]
        [InlineData(WebSocketState.CloseSent)]
        [InlineData(WebSocketState.CloseReceived)]
        [InlineData(WebSocketState.Aborted)]
        [InlineData(WebSocketState.Connecting)]
        public async Task SendNoopsIfNotOpen(WebSocketState state)
        {
            var webSocket = new Mock<WebSocket>();
            var webSocketHandler = new Mock<WebSocketHandler>(64 * 1024) { CallBase = true};

            webSocket.Setup(m => m.State).Returns(state);
            webSocketHandler.Object.WebSocket = webSocket.Object;

            await webSocketHandler.Object.SendAsync("Hello");

            webSocket.Verify(m => m.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, CancellationToken.None), Times.Never());
        }

        [Fact]
        public async Task DefaultWebSocketHandlerOperationsNoopAfterClose()
        {
            var handler = new DefaultWebSocketHandler(maxIncomingMessageSize: null);

            var initialWebSocket = new Mock<WebSocket>();

            initialWebSocket.Setup(w => w.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None))
                .Returns(TaskAsyncHelper.Empty);

            initialWebSocket.Setup(w => w.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None))
                .Returns(TaskAsyncHelper.Empty);

            initialWebSocket.Setup(w => w.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), CancellationToken.None))
                     .Returns(Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, endOfMessage: true)));

            await handler.ProcessWebSocketRequestAsync(initialWebSocket.Object, CancellationToken.None);
            
            // Swap the socket here so we can verify what happens after the task returns
            var afterWebSocket = new Mock<WebSocket>();

            handler.WebSocket = afterWebSocket.Object;

            await handler.Send("Hello World");
            await handler.CloseAsync();

            afterWebSocket.Verify(m => m.State, Times.Never());
            afterWebSocket.Verify(m => m.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, CancellationToken.None), Times.Never());
            afterWebSocket.Verify(m => m.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None), Times.Never());
        }
    }
}
