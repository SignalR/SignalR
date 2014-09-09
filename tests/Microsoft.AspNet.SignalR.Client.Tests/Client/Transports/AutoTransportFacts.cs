
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Moq;
using System.Threading.Tasks;
using Moq.Protected;
using Xunit;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class AutoTransportFacts
    {
        [Fact]
        public void AutoTransportDoesNotTryAnotherTransportIfTransportFailsDuringStartRequest()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(5));
            mockConnection.Setup(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());

            var mockHttpClient = new Mock<IHttpClient>();

            var mockFailingTransport = new Mock<ClientTransportBase>(mockHttpClient.Object, "fakeTransport") {CallBase = true};
            mockFailingTransport.Protected()
                .Setup("OnStart", ItExpr.IsAny<IConnection>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .Callback<IConnection, string, CancellationToken>((c, d, t) =>
                {
                    mockFailingTransport.Object.ProcessResponse(c,
                        "{\"C\":\"d-C6243495-A,0|B,0|C,1|D,0\",\"S\":1,\"M\":[]}");
                });

            var exception = new Exception("test exception");
            mockHttpClient
                .Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<bool>()))
                .Returns<string, Action<IRequest>, bool>((url, prepareRequest, isLongRunning) =>
                {
                    mockFailingTransport.Object.TransportFailed(exception);

                    return Task.FromResult(Mock.Of<IResponse>());
                });

            var mockTransport = new Mock<IClientTransport>();
            mockTransport.Setup(t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(null));

            var autoTransport = new AutoTransport(mockHttpClient.Object,
                new List<IClientTransport> {mockFailingTransport.Object, mockTransport.Object});

            var startException =
                Assert.Throws<AggregateException>(() =>
                    autoTransport.Start(mockConnection.Object, string.Empty, CancellationToken.None)
                        .Wait(TimeSpan.FromSeconds(1))).InnerException;

            Assert.IsType<StartException>(startException);
            Assert.Same(exception, startException.InnerException);

            mockTransport.Verify(
                t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        public void AutoTransportDoesNotTryAnotherTransportIfStartRequestFails()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(5));
            mockConnection.Setup(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());

            var mockHttpClient = new Mock<IHttpClient>();

            var mockFailingTransport = new Mock<ClientTransportBase>(mockHttpClient.Object, "fakeTransport") { CallBase = true };
            mockFailingTransport.Protected()
                .Setup("OnStart", ItExpr.IsAny<IConnection>(), ItExpr.IsAny<string>(), ItExpr.IsAny<CancellationToken>())
                .Callback<IConnection, string, CancellationToken>((c, d, t) =>
                {
                    mockFailingTransport.Object.ProcessResponse(c,
                        "{\"C\":\"d-C6243495-A,0|B,0|C,1|D,0\",\"S\":1,\"M\":[]}");
                });

            var exception = new Exception("test exception");
            mockHttpClient
                .Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<bool>()))
                .Returns<string, Action<IRequest>, bool>((url, prepareRequest, isLongRunning) =>
                {
                    var tcs = new TaskCompletionSource<IResponse>();
                    tcs.SetException(exception);
                    return tcs.Task;
                });

            var mockTransport = new Mock<IClientTransport>();
            mockTransport.Setup(t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<object>(null));

            var autoTransport = new AutoTransport(mockHttpClient.Object,
                new List<IClientTransport> { mockFailingTransport.Object, mockTransport.Object });

            var startException =
                Assert.Throws<AggregateException>(() =>
                    autoTransport.Start(mockConnection.Object, string.Empty, CancellationToken.None)
                        .Wait(TimeSpan.FromSeconds(1))).InnerException;

            Assert.IsType<StartException>(startException);
            // startException.InnerException is an AggregateException
            Assert.Same(exception, startException.InnerException.InnerException);

            mockTransport.Verify(
                t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

    }
}
