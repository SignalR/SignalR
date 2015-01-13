using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketRequestFacts
    {
        [Fact]
        public void SetRequestHeadersSetsHeadersOnWebSockets()
        {
            var webSocket = new FakeWebSocket();
            var webSocketRequest = new WebSocketRequest(webSocket);

            webSocketRequest.SetRequestHeaders(
                new Dictionary<string, string> { { "Header1", "Value1" }, { "Header2", "Value2" } });

            webSocket.Verify(
                "SetRequestHeader",
                new List<object[]>
                {
                    new object[] {"Header1", "Value1"},
                    new object[] {"Header2", "Value2"},
                });
        }

        [Fact]
        public void UserAgentAlwaysReturnsNull()
        {
            var webSocketRequest = new WebSocketRequest(new FakeWebSocket());

            Assert.Null(webSocketRequest.UserAgent);

            webSocketRequest.UserAgent = "Agent";

            Assert.Null(webSocketRequest.UserAgent);
        }

        [Fact]
        public void AcceptAlwaysReturnsNull()
        {
            var webSocketRequest = new WebSocketRequest(new FakeWebSocket());

            Assert.Null(webSocketRequest.Accept);

            webSocketRequest.UserAgent = "Rejected";

            Assert.Null(webSocketRequest.Accept);
        }
    }
}
