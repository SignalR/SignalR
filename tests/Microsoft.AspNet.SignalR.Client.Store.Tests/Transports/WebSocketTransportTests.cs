// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransportTests
    {
        [Fact]
        public void MaxIncomingMessageSizeNullForWebSocketTransport()
        {
            Assert.Null(new WebSocketTransport().MaxIncomingMessageSize);
        }

        [Fact]
        public void NameReturnsCorrectTransportName()
        {
            Assert.Equal("webSockets", new WebSocketTransport().Name);
        }

        [Fact]
        public void SupportsKeepAliveReturnsTrue()
        {
            Assert.True(new WebSocketTransport().SupportsKeepAlive);
        }
    }
}