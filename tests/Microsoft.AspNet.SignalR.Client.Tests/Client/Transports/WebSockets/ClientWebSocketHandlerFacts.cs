
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.WebSockets;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Transports.WebSockets
{
    public class ClientWebSocketHandlerFacts
    {
        [Fact]
        public void OnOpenCallsIntoWebSocketTransportOnMessage()
        {
            var mockWebSocketTransport = new Mock<WebSocketTransport>();

            var webSocketHandler = new ClientWebSocketHandler(mockWebSocketTransport.Object);
            webSocketHandler.OnOpen();

            mockWebSocketTransport.Verify(p => p.OnOpen(), Times.Once());
        }

        [Fact]
        public void OnMessageCallsIntoWebSocketTransportOnMessage()
        {
            var mockWebSocketTransport = new Mock<WebSocketTransport>();

            var webSocketHandler = new ClientWebSocketHandler(mockWebSocketTransport.Object);
            webSocketHandler.OnMessage("msg");

            mockWebSocketTransport.Verify(p => p.OnMessage("msg"), Times.Once());
        }

        [Fact]
        public void OnCloseCallsIntoWebSocketTransportOnClose()
        {
            var mockWebSocketTransport = new Mock<WebSocketTransport>();

            var webSocketHandler = new ClientWebSocketHandler(mockWebSocketTransport.Object);
            webSocketHandler.OnClose();

            mockWebSocketTransport.Verify(p => p.OnClose(), Times.Once());
        }

        [Fact]
        public void OnErrorCallsIntoWebSocketTransportOnErrorAndPassesException()
        {
            var exception = new Exception();
            var mockWebSocketTransport = new Mock<WebSocketTransport>();

            var webSocketHandler =
                new ClientWebSocketHandler(mockWebSocketTransport.Object) { Error = exception };

            webSocketHandler.OnError();

            mockWebSocketTransport.Verify(p => p.OnError(exception), Times.Once());
        }

        [Fact]
        public async Task WebSocketHandlerClosesIfWebSocketStateIsCloseSentAfterClosing()
        {
            var messageIndex = 0;
            var webSocketMessages = new[] { new WebSocketReceiveResult(0, WebSocketMessageType.Text, endOfMessage: false),
                                            new WebSocketReceiveResult(0, WebSocketMessageType.Text, endOfMessage: false),
                                            new WebSocketReceiveResult(0, WebSocketMessageType.Close, endOfMessage: true)};

            var webSocket = new Mock<WebSocket>(MockBehavior.Strict);

            webSocket.Setup(w => w.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), CancellationToken.None))
                     .Returns(() => TaskAsyncHelper.FromResult(webSocketMessages[messageIndex++]));

            WebSocketState state = WebSocketState.Open;
            webSocket.Setup(w => w.State).Returns(() => state);
            webSocket.Setup(w => w.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None))
                .Returns(() =>
                {
                    state = WebSocketState.CloseSent;
                    return TaskAsyncHelper.Empty;
                });

            webSocket.Setup(w => w.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None))
                .Returns(() =>
                {
                    state = WebSocketState.Closed;
                    return TaskAsyncHelper.Empty;
                });
            webSocket.As<IDisposable>().Setup(w => w.Dispose());

            var webSocketHandler = new Mock<WebSocketHandler>(64 * 1024) { CallBase = true };
            await webSocketHandler.Object.ProcessWebSocketRequestAsync(webSocket.Object, CancellationToken.None);

            webSocket.VerifyAll();
        }
    }
}
