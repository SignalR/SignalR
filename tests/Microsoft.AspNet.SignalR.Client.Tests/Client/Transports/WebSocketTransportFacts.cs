// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransportFacts
    {
        [Fact]
        public void TransportInitializationHandlerStoppedIfWebsocketClosedWhenConnecting()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.State).Returns(ConnectionState.Connecting);
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(new TimeSpan(0, 0, 5));

            var mockWebSocketTransport = new Mock<WebSocketTransport> { CallBase = true };
            var webSocketTransport = mockWebSocketTransport.Object;

            mockWebSocketTransport.Setup(t => t.PerformConnect())
                .Callback(webSocketTransport.OnClose)
                .Returns(Task.FromResult(0));

            var ex = Assert.Throws<AggregateException>(
                () => webSocketTransport.Start(mockConnection.Object, "test", new CancellationToken()).Wait());

            Assert.Equal(Resources.Error_TransportFailedToConnect, ex.InnerException.Message);
        }
    }
}
