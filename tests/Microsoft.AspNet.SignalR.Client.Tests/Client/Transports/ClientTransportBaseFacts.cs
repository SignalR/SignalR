
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Moq;
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
                new Mock<ClientTransportBase>(client, transportName, mockTransportHelper.Object)
                {
                    CallBase = true
                }.Object;

            Assert.Same(negotiationResponse, await transport.Negotiate(connection, "test"));

            mockTransportHelper.Verify(m => m.GetNegotiationResponse(client, connection, "test"), Times.Once());
        }

        [Fact]
        public void FinishedInitializedToFalse()
        {
            var transport =
                new Mock<ClientTransportBase>(Mock.Of<IHttpClient>(), "fakeTransport", Mock.Of<TransportHelper>())
                    .Object;

            Assert.False(transport.Finished);
        }

        [Fact]
        public void AbortValidatesArguments()
        {
            var transport =
                new Mock<ClientTransportBase>(Mock.Of<IHttpClient>(), "fakeTransport", Mock.Of<TransportHelper>())
                {
                    CallBase = true
                }.Object;

            Assert.Equal("connection",
                Assert.Throws<ArgumentNullException>(() => transport.Abort(null, "connectionData")).ParamName);
        }

        [Fact]
        public void AbortRequestNotSentIfConnectionTokenNull()
        {
            var mockClient = new Mock<IHttpClient>();
            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>())
                {
                    CallBase = true
                }.Object;

            transport.Abort(Mock.Of<IConnection>(), "connectionData");

            mockClient.Verify(
                m => m.Post(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()),
                Times.Never());

            Assert.True(transport.Finished);            
        }

        [Fact]
        public void AbortSendsAbortRequest()
        {
            var connection = new Connection("http://fake.url/") { ConnectionToken = "connectionToken"};
            
            var mockClient = new Mock<IHttpClient>();
            mockClient
                .Setup(m => m.Post(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(Task.FromResult((IResponse) null));

            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>())
                {
                    CallBase = true
                }.Object;

            transport.Abort(connection, "connectionData");

            mockClient.Verify(
                m => m.Post(It.Is<string>(url => url.StartsWith("http://fake.url/abort?")), It.IsAny<Action<IRequest>>(),
                        It.IsAny<IDictionary<string, string>>(), false),
                Times.Once());

            Assert.True(transport.Finished);
        }

        [Fact]
        public void AbortSendsAbortRequestOnlyOnce()
        {
            var connection = new Connection("http://fake.url/") { ConnectionToken = "connectionToken" };

            var mockClient = new Mock<IHttpClient>();
            mockClient
                .Setup(m => m.Post(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(Task.FromResult((IResponse)null));

            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>())
                {
                    CallBase = true
                }.Object;

            transport.Abort(connection, "connectionData");
            Assert.True(transport.Finished);

            transport.Abort(connection, "connectionData");

            mockClient.Verify(
                m => m.Post(It.Is<string>(url => url.StartsWith("http://fake.url/abort?")), It.IsAny<Action<IRequest>>(),
                        It.IsAny<IDictionary<string, string>>(), false),
                Times.Once());

            Assert.True(transport.Finished);
        }

        [Fact]
        public void FailuresWhileSendingAbortRequestsAreLoggedAndSwallowed()
        {
            const string exceptionMessage = "Abort request failed";

            var tcs = new TaskCompletionSource<IResponse>();
            tcs.SetException(new InvalidOperationException(exceptionMessage));

            var traceStringBuilder = new StringBuilder();
            var connection = new Connection("http://fake.url/")
            {
                ConnectionToken = "connectionToken",
                TraceWriter = new StringWriter(traceStringBuilder)
            };

            var mockClient = new Mock<IHttpClient>();
            mockClient
                .Setup(m => m.Post(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>())
                {
                    CallBase = true
                }.Object;

            transport.Abort(connection, "connectionData");

            mockClient.Verify(
                m => m.Post(It.Is<string>(url => url.StartsWith("http://fake.url/abort?")), It.IsAny<Action<IRequest>>(),
                        It.IsAny<IDictionary<string, string>>(), false),
                Times.Once());

            Assert.True(transport.Finished);
            Assert.Contains(exceptionMessage, traceStringBuilder.ToString());
        }

        [Fact]
        public void DisposeMarksTransportAsFinished()
        {
            var transport = 
                new Mock<ClientTransportBase>(Mock.Of<IHttpClient>(), "fakeTransport") { CallBase = true}.Object;

            transport.Dispose();

            Assert.True(transport.Finished);
        }

        [Fact]
        public void CannotNegotiateUsingFinishedTransport()
        {
            var transport =
                new Mock<ClientTransportBase>(Mock.Of<IHttpClient>(), "fakeTransport") {CallBase = true}.Object;

            transport.Dispose();

            Assert.Equal(
                Resources.Error_TransportCannotBeReused,
                Assert.Throws<InvalidOperationException>(
                    () => transport.Negotiate(Mock.Of<IConnection>(), "connectionData")).Message);
        }
    }
}
