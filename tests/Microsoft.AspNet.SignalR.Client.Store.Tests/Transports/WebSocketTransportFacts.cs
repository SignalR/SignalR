// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransportFacts
    {
        [Fact]
        public void CannotCreateWebSocketTransportWithNullHttpClient()
        {
            Assert.Equal(
                "httpClient",
                Assert.Throws<ArgumentNullException>(() => new WebSocketTransport(null)).ParamName);
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

        [Fact]
        public async Task NegotiateInvokesGetNegotiationResponseOnTransportHelperAsync()
        {
            var connection = new Connection("http://fake.url/");
            var client = new DefaultHttpClient();
            var negotiationResponse = new NegotiationResponse();

            var transportHelper = new FakeTransportHelper(client, connection, "test", negotiationResponse);

            Assert.Same(
                negotiationResponse,
                await new WebSocketTransport(client, transportHelper).Negotiate(connection, "test"));
        }
    }
}