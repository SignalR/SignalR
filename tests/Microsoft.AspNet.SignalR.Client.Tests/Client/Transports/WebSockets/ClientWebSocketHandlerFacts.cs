
using System;
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
    }
}
