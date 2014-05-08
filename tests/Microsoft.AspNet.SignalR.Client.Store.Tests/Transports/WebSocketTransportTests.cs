// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.AspNet.SignalR.Client.Transports;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Store.Transports
{
    public class WebSocketTransportTests
    {
        [Fact]
        public void NameReturnsCorrectTransportName()
        {
            Assert.Equal("webSockets", new WebSocketTransport().Name);
        }
    }
}
