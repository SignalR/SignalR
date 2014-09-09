
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Microsoft.AspNet.SignalR.Client.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class ClientTransportBaseFacts
    {
        [Fact]
        public void ProcessResponseCapturesOnReceivedExceptions()
        {
            var ex = new Exception();
            var connection = new Mock<IConnection>(MockBehavior.Strict);
            connection.SetupGet(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());
            connection.Setup(c => c.OnReceived(It.IsAny<JToken>())).Throws(ex);
            connection.Setup(c => c.OnError(ex));
            connection.Setup(c => c.MarkLastMessage());

            var transport =
                new Mock<ClientTransportBase>(Mock.Of<IHttpClient>(), "fakeTransport") {CallBase = true}.Object;

            transport.Start(Mock.Of<IConnection>(), string.Empty, CancellationToken.None);

            // PersistentResponse
            transport.ProcessResponse(connection.Object, "{\"M\":{}}");

            // HubResponse (WebSockets)
            transport.ProcessResponse(connection.Object, "{\"I\":{}}");

            connection.VerifyAll();
        }

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
        public void AbortValidatesArguments()
        {
            var httpClient = Mock.Of<IHttpClient>();
            var abortHandler = new TransportAbortHandler(httpClient, "fakeTransport");

            var transport =
                new Mock<ClientTransportBase>(httpClient, "fakeTransport", Mock.Of<TransportHelper>(), abortHandler)
                {
                    CallBase = true
                }.Object;

            Assert.Equal("connection",
                Assert.Throws<ArgumentNullException>(() => transport.Abort(null, new TimeSpan(0, 0, 5), "connectionData")).ParamName);
        }

        [Fact]
        public void AbortRequestNotSentIfConnectionTokenNull()
        {
            var mockClient = new Mock<IHttpClient>();
            var abortHandler = new TransportAbortHandler(mockClient.Object, "fakeTransport");
            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>(), abortHandler)
                {
                    CallBase = true
                }.Object;

            transport.Abort(Mock.Of<IConnection>(), new TimeSpan(0, 0, 5), "connectionData");

            mockClient.Verify(
                m => m.Post(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()),
                Times.Never());
        }

        [Fact]
        public void AbortSendsAbortRequest()
        {
            var connection = new Connection("http://fake.url/") { ConnectionToken = "connectionToken"};
            
            var mockClient = new Mock<IHttpClient>();

            var abortHandler = new TransportAbortHandler(mockClient.Object, "fakeTransport");

            mockClient
                .Setup(m => m.Post(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(() => 
                {
                    abortHandler.CompleteAbort();
                    return Task.FromResult((IResponse)null); 
                });

            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>(), abortHandler)
                {
                    CallBase = true
                }.Object;

            transport.Abort(connection, new TimeSpan(0, 0, 5), "connectionData");

            mockClient.Verify(
                m => m.Post(It.Is<string>(url => url.StartsWith("http://fake.url/abort?")), It.IsAny<Action<IRequest>>(),
                        It.IsAny<IDictionary<string, string>>(), false),
                Times.Once());
        }

        [Fact]
        public void AbortSendsAbortRequestOnlyOnce()
        {
            var connection = new Connection("http://fake.url/") { ConnectionToken = "connectionToken" };

            var mockClient = new Mock<IHttpClient>();

            var abortHandler = new TransportAbortHandler(mockClient.Object, "fakeTransport");

            mockClient
                .Setup(m => m.Post(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(() =>
                {
                    abortHandler.CompleteAbort();
                    return Task.FromResult((IResponse)null);
                });

            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>(), abortHandler)
                {
                    CallBase = true
                }.Object;

            transport.Abort(connection, new TimeSpan(0, 0, 5), "connectionData");

            transport.Abort(connection, new TimeSpan(0, 0, 5), "connectionData");

            mockClient.Verify(
                m => m.Post(It.Is<string>(url => url.StartsWith("http://fake.url/abort?")), It.IsAny<Action<IRequest>>(),
                        It.IsAny<IDictionary<string, string>>(), false),
                Times.Once());
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

            var abortHandler = new TransportAbortHandler(mockClient.Object, "fakeTransport");

            var transport =
                new Mock<ClientTransportBase>(mockClient.Object, "fakeTransport", Mock.Of<TransportHelper>(), abortHandler)
                {
                    CallBase = true
                }.Object;

            transport.Abort(connection, new TimeSpan(0, 0, 5), "connectionData");

            mockClient.Verify(
                m => m.Post(It.Is<string>(url => url.StartsWith("http://fake.url/abort?")), It.IsAny<Action<IRequest>>(),
                        It.IsAny<IDictionary<string, string>>(), false),
                Times.Once());

            Assert.Contains(exceptionMessage, traceStringBuilder.ToString());
        }

        [Fact]
        public void CannotNegotiateUsingFinishedTransport()
        {
            var transport =
                new Mock<ClientTransportBase>(Mock.Of<IHttpClient>(), "fakeTransport") { CallBase = true }.Object;

            transport.Dispose();

            Assert.Equal(
                Resources.Error_TransportCannotBeReused,
                Assert.Throws<InvalidOperationException>(
                    () => transport.Negotiate(Mock.Of<IConnection>(), "connectionData")).Message);
        }

        [Fact]
        public void CannotInvokeProcessResponseBeforeStartingTransport()
        {
            Assert.Equal(
                Resources.Error_ProcessResponseBeforeStart,
                Assert.Throws<InvalidOperationException>(
                    () => new Mock<ClientTransportBase>(Mock.Of<IHttpClient>(), "fakeTransport") {CallBase = true}.Object
                            .ProcessResponse(Mock.Of<IConnection>(), "{}")).Message);
        }
    }
}
