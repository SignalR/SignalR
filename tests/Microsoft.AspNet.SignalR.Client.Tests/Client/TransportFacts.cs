using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Client.Transports.WebSockets;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class TransportFacts
    {
        [Fact]
        public void CancelledTaskHandledinServerSentEvents()
        {
            var tcs = new TaskCompletionSource<IResponse>();
            tcs.TrySetCanceled();

            var httpClient = new Mock<IHttpClient>();
            var connection = new Mock<IConnection>();

            httpClient.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Action<IRequest>>(), It.IsAny<bool>()))
                .Returns<string, Action<IRequest>, bool>((url, prepareRequest, isLongRunning) =>
                {
                    prepareRequest(Mock.Of<IRequest>());
                    return tcs.Task;
                });

            connection.SetupGet(c => c.ConnectionToken).Returns("foo");
            connection.SetupGet(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(15));

            var sse = new ServerSentEventsTransport(httpClient.Object);

            var exception = Assert.Throws<AggregateException>(
                () => sse.Start(connection.Object, string.Empty, CancellationToken.None).Wait(TimeSpan.FromSeconds(5)));

            Assert.IsType(typeof(OperationCanceledException), exception.InnerException);
        }

        [Fact]
        public void CancelledTaskHandledinAutoTransport()
        {
            var tcs = new TaskCompletionSource<IResponse>();

            tcs.TrySetCanceled();

            var transport = new Mock<IClientTransport>();
            transport.Setup(t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), CancellationToken.None))
                .Returns(tcs.Task);

            var transports = new List<IClientTransport>();
            transports.Add(transport.Object);

            var autoTransport = new AutoTransport(new DefaultHttpClient(), transports);
            var task = autoTransport.Start(new Connection("http://foo"), string.Empty, CancellationToken.None);

            Assert.IsType(typeof(OperationCanceledException), task.Exception.InnerException);
        }

        [Fact]
        public void StartExceptionStopsAutoTransportFallback()
        {
            var errorTcs = new TaskCompletionSource<IResponse>();
            errorTcs.SetException(new StartException());

            var failingTransport = new Mock<IClientTransport>();
            failingTransport.Setup(t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), CancellationToken.None))
                .Returns(errorTcs.Task)
                .Verifiable();

            var unusedTransport = new Mock<IClientTransport>();

            var transports = new List<IClientTransport>();
            transports.Add(failingTransport.Object);
            transports.Add(unusedTransport.Object);

            var autoTransport = new AutoTransport(new DefaultHttpClient(), transports);
            var startTask = autoTransport.Start(new Connection("http://foo"), string.Empty, CancellationToken.None);

            failingTransport.Verify();
            unusedTransport.Verify(t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), CancellationToken.None), Times.Never());

            Assert.IsType(typeof(StartException), startTask.Exception.InnerException);
        }

        [Fact]
        public void CancelledTaskHandledWhenStartingLongPolling()
        {
            var tcs = new TaskCompletionSource<IResponse>();
            tcs.SetCanceled();

            var httpClient = new Mock<IHttpClient>();

            httpClient.Setup(c => c.Post(It.IsAny<string>(),
                It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(15));

            var longPollingTransport = new LongPollingTransport(httpClient.Object);

            var unwrappedException = Assert.Throws<AggregateException>(() =>
                longPollingTransport.Start(mockConnection.Object, null, CancellationToken.None)
                    .Wait(TimeSpan.FromSeconds(5))).InnerException;

            Assert.IsType<OperationCanceledException>(unwrappedException);
        }

        [Fact]
        public void CancelledTaskHandledinLongPollingLoop()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(1500));

            var tcs = new TaskCompletionSource<IResponse>();
            tcs.SetCanceled();

            var onErrorWh = new ManualResetEvent(false);

            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.Setup(c => c.Post(It.IsAny<string>(),
                It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(() => tcs.Task);

            var mockLongPollingTransport = new Mock<LongPollingTransport>(mockHttpClient.Object) { CallBase = true };
            mockLongPollingTransport.Setup(t => t.OnError(It.IsAny<IConnection>(), It.IsAny<Exception>()))
                .Callback(() => onErrorWh.Set());

            mockLongPollingTransport.Object.StartPolling(mockConnection.Object, string.Empty);

            Assert.True(onErrorWh.WaitOne(TimeSpan.FromSeconds(2)));

            mockLongPollingTransport
                .Verify(
                    t => t.OnError(It.IsAny<IConnection>(),
                            It.Is<OperationCanceledException>(e => string.Equals(e.Message, Resources.Error_TaskCancelledException))),
                    Times.Once());
        }

        [Fact]
        public void WebSocketRemovedFromTransportList()
        {
            var tcs = new TaskCompletionSource<NegotiationResponse>();
            var mre = new ManualResetEventSlim();

            var transports = new List<IClientTransport>();

            var webSocketTransport = new Mock<WebSocketTransport>();
            webSocketTransport.Protected()
                .Setup("OnStart", ItExpr.IsAny<IConnection>(), ItExpr.IsAny<string>(), CancellationToken.None)
                .Callback(mre.Set);
            
            transports.Add(webSocketTransport.Object);
            transports.Add(new ServerSentEventsTransport());
            transports.Add(new LongPollingTransport());

            var negotiationResponse = new NegotiationResponse();
            negotiationResponse.TryWebSockets = false;

            tcs.SetResult(negotiationResponse);

            var autoTransport = new Mock<AutoTransport>(It.IsAny<IHttpClient>(), transports);
            autoTransport.Setup(c => c.GetNegotiateResponse(It.IsAny<Connection>(), It.IsAny<string>())).Returns(tcs.Task);
            autoTransport.Object.Negotiate(new Connection("http://foo", string.Empty), string.Empty).Wait();

            Assert.False(mre.IsSet);
        }

        [Fact]
        public void SendCatchesOnReceivedExceptions()
        {
            var ex = new Exception();
            var wh = new ManualResetEventSlim();
            var response = new Mock<IResponse>(MockBehavior.Strict);
            var httpClient = new Mock<IHttpClient>(MockBehavior.Strict);
            var connection = new Mock<Client.IConnection>(MockBehavior.Strict);

            using (var mockStream = new MemoryStream())
            {
                using (var sw = new StreamWriter(mockStream))
                {
                    sw.Write("{}");
                    sw.Flush();
                    mockStream.Position = 0;

                    response.Setup(r => r.GetStream()).Returns(mockStream);
                    response.Setup(r => r.Dispose());

                    httpClient.Setup(h => h.Post(It.IsAny<string>(),
                                                 It.IsAny<Action<Client.Http.IRequest>>(),
                                                 It.IsAny<IDictionary<string, string>>(), false))
                              .Returns(TaskAsyncHelper.FromResult(response.Object));

                    connection.Setup(c => c.Trace(TraceLevels.Messages, It.IsAny<string>(), It.IsAny<object[]>()));
                    connection.SetupGet(c => c.Url).Returns("");
                    connection.SetupGet(c => c.Protocol).Returns(new Version());
                    connection.SetupGet(c => c.QueryString).Returns("");
                    connection.SetupGet(c => c.ConnectionToken).Returns("");
                    connection.SetupGet(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());
                    connection.Setup(c => c.OnReceived(It.IsAny<JToken>())).Throws(ex);
                    connection.Setup(c => c.OnError(It.IsAny<AggregateException>())).Callback<Exception>(e =>
                    {
                        Assert.Equal(ex, e.InnerException);
                        wh.Set();
                    });

                    var httpBasedTransport = new Mock<HttpBasedTransport>(httpClient.Object, "fakeTransport")
                    {
                        CallBase = true
                    };

                    httpBasedTransport.Object.Send(connection.Object, "", null).ContinueWith(sendTask =>
                    {
                        Assert.True(sendTask.IsFaulted);
                        Assert.IsType(typeof(AggregateException), sendTask.Exception);
                        Assert.Equal(ex, sendTask.Exception.InnerException);
                        Assert.True(wh.Wait(TimeSpan.FromSeconds(1)));
                    }).Wait();

                    response.VerifyAll();
                    httpClient.VerifyAll();
                    connection.VerifyAll();
                }
            }
        }

        [Theory]
        [InlineData(WebSocketState.Aborted)]
        [InlineData(WebSocketState.Closed)]
        [InlineData(WebSocketState.CloseReceived)]
        [InlineData(WebSocketState.CloseSent)]
        [InlineData(WebSocketState.Connecting)]
        [InlineData(WebSocketState.Connecting)]
        public void WebSocketSendReturnsAFaultedTaskWhenNotConnected(WebSocketState state)
        {
            var mockConnection = new Mock<Client.IConnection>(MockBehavior.Strict);
            var mockWebSocket = new Mock<WebSocket>(MockBehavior.Strict);
            var mockWebSocketHandler = new Mock<ClientWebSocketHandler>();

            mockWebSocket.SetupGet(ws => ws.State).Returns(state);
            mockConnection.Setup(c => c.OnError(It.IsAny<InvalidOperationException>()));
            mockWebSocketHandler.Object.WebSocket = mockWebSocket.Object;
            
            var wsTransport = new WebSocketTransport(mockWebSocketHandler.Object);

            var task = wsTransport.Send(mockConnection.Object, "", "");

            Assert.True(task.IsFaulted);
            Assert.IsType(typeof(InvalidOperationException), task.Exception.InnerException);

            mockConnection.VerifyAll();
            mockWebSocket.VerifyAll();
        }

        [Fact]
        public void AllTransportsButLongPollingSupportKeepAlives()
        {
            Assert.True(new WebSocketTransport().SupportsKeepAlive);
            Assert.True(new ServerSentEventsTransport().SupportsKeepAlive);
            Assert.False(new LongPollingTransport().SupportsKeepAlive);
        }
    }
}
