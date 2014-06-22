
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class ClientTransportBaseFacts
    {
        [Fact]
        public async Task NegotiateInvokesGetNegotiationResponseOnTransportHelperAsync()
        {
            const string transportName = "fakeTransport";

            var connection = new Connection("http://fake.url/");
            var client = new DefaultHttpClient();
            var negotiationResponse = new NegotiationResponse();

            var mockTransportHelper = new Mock<TransportHelper>();
            mockTransportHelper.Setup(
                h => h.GetNegotiationResponse(It.IsAny<IHttpClient>(), It.IsAny<IConnection>(), It.IsAny<string>()))
                .Returns(Task.FromResult(negotiationResponse));

            var transport =
                new Mock<ClientTransportBase>(client, transportName, mockTransportHelper.Object,
                    new TransportAbortHandler(client, transportName))
                {
                    CallBase = true
                }.Object;

            Assert.Same(negotiationResponse, await transport.Negotiate(connection, "test"));

            mockTransportHelper.Verify(m => m.GetNegotiationResponse(client, connection, "test"), Times.Once());
        }

        [Fact]
        public void AbortInvokesAbortOnAbortHandler()
        {
            const string transportName = "fakeTransport";
            var httpClient = Mock.Of<IHttpClient>();
            var mockAbortHandler =
                new Mock<TransportAbortHandler>(Mock.Of<IHttpClient>(), transportName);
            var mockTransportBase = new Mock<ClientTransportBase>(httpClient, transportName, new TransportHelper(), mockAbortHandler.Object)
            {
                CallBase = true 
            };

            var connection = Mock.Of<IConnection>();
            var timeSpan = new TimeSpan(42);
            const string connectionData = "connData";

            mockTransportBase.Object.Abort(connection, timeSpan, connectionData);

            mockAbortHandler.Verify(h => h.Abort(connection, timeSpan, connectionData), Times.Once());
        }

        [Fact]
        public void DisposeDisposesAbortHandler()
        {
            const string transportName = "fakeTransport";
            var httpClient = Mock.Of<IHttpClient>();
            var mockAbortHandler = 
                new Mock<TransportAbortHandler>(Mock.Of<IHttpClient>(), transportName);
            var mockTransportBase =
                new Mock<ClientTransportBase>(httpClient, transportName, new TransportHelper(), mockAbortHandler.Object)
                {
                    CallBase = true
                };

            mockTransportBase.Object.Dispose();

            mockAbortHandler.Protected().Verify("Dispose", Times.Once(), true);
        }
    }
}
