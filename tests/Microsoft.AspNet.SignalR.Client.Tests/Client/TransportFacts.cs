using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class TransportFacts
    {
        [Theory]
        [InlineData("bob=12345", "&bob=12345")]
        [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
        [InlineData("", "")]
        [InlineData(null, "?transport=&connectionToken=")]
        [InlineData("?foo=bar", "?foo=bar")]
        [InlineData("?foo=bar&baz=bear", "?foo=bar&baz=bear")]
        [InlineData("&foo=bar", "&foo=bar")]
        [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
        public void GetReceiveQueryStringAppendsConnectionQueryString(string connectionQs, string expected)
        {
            var connection = new Connection("http://foo.com", connectionQs);
            connection.ConnectionToken = "";

            var urlQs = TransportHelper.GetReceiveQueryString(connection, null, "");

            Assert.True(urlQs.EndsWith(expected));
        }

        [Theory]
        [InlineData("bob=12345", "?bob=12345")]
        [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "?bob=12345&foo=leet&baz=laskjdflsdk")]
        [InlineData("", "")]
        [InlineData(null, "")]
        [InlineData("?foo=bar", "?foo=bar")]
        [InlineData("?foo=bar&baz=bear", "?foo=bar&baz=bear")]
        [InlineData("&foo=bar", "&foo=bar")]
        [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
        public void AppendCustomQueryStringAppendsConnectionQueryString(string connectionQs, string expected)
        {
            var connection = new Connection("http://foo.com", connectionQs);

            var urlQs = TransportHelper.AppendCustomQueryString(connection, "http://foo.com");

            Assert.Equal(urlQs, expected);
        }

        [Fact]
        public void OnInitializedFiresFromInitializeMessage()
        {
            bool timedOut, disconnected, triggered = false;
            var connection = new Connection("http://foo.com");

            TransportHelper.ProcessResponse(connection, "{\"S\":1, \"M\":[]}", out timedOut, out disconnected, () =>
            {
                triggered = true;
            });

            Assert.True(triggered);
        }

        [Fact]
        public void ProcessResponseCapturesOnReceivedExceptions()
        {
            bool timedOut, disconnected;
            var ex = new Exception();
            var connection = new Mock<Client.IConnection>(MockBehavior.Strict);
            connection.Setup(c => c.OnReceived(It.IsAny<JToken>())).Throws(ex);
            connection.Setup(c => c.OnError(ex));
            connection.Setup(c => c.MarkLastMessage());

            // PersistentResponse
            TransportHelper.ProcessResponse(connection.Object, "{\"M\":{}}", out timedOut, out disconnected);

            // HubResponse (WebSockets)
            TransportHelper.ProcessResponse(connection.Object, "{\"I\":{}}", out timedOut, out disconnected);

            connection.VerifyAll();
        }

        [Fact]
        public void CancelledTaskHandledinServerSentEvents()
        {
            var tcs = new TaskCompletionSource<IResponse>();
            var wh = new TaskCompletionSource<Exception>();

            tcs.TrySetCanceled();

            var httpClient = new Mock<Microsoft.AspNet.SignalR.Client.Http.IHttpClient>();
            var connection = new Mock<Microsoft.AspNet.SignalR.Client.IConnection>();

            httpClient.Setup(c => c.Get(It.IsAny<string>(),
                It.IsAny<Action<Client.Http.IRequest>>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            connection.SetupGet(c => c.ConnectionToken).Returns("foo");

            var sse = new ServerSentEventsTransport(httpClient.Object);
            sse.OpenConnection(connection.Object, (ex) =>
            {
                wh.TrySetResult(ex);
            });

            Assert.True(wh.Task.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsType(typeof(OperationCanceledException), wh.Task.Result);
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
        public void CancelledTaskHandledinLongPolling()
        {
            var tcs = new TaskCompletionSource<IResponse>();
            var wh = new TaskCompletionSource<Exception>();

            tcs.TrySetCanceled();

            var httpClient = new Mock<Microsoft.AspNet.SignalR.Client.Http.IHttpClient>();

            httpClient.Setup(c => c.Post(It.IsAny<string>(),
                It.IsAny<Action<Client.Http.IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            var pollingHandler = new PollingRequestHandler(httpClient.Object);
            pollingHandler.Start();

            pollingHandler.OnError += (ex) => { wh.TrySetResult(ex); };

            Assert.True(wh.Task.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsType(typeof(OperationCanceledException), wh.Task.Result);
        }

        [Fact]
        public void WebSocketRemovedFromTransportList()
        {
            var tcs = new TaskCompletionSource<NegotiationResponse>();
            var mre = new ManualResetEventSlim();

            var transports = new List<IClientTransport>();

            var webSocketTransport = new Mock<WebSocketTransport>();
            webSocketTransport.Setup(w => w.Start(It.IsAny<IConnection>(), It.IsAny<string>(), CancellationToken.None))
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
                    connection.SetupGet(c => c.QueryString).Returns("");
                    connection.SetupGet(c => c.ConnectionToken).Returns("");
                    connection.Setup(c => c.OnReceived(It.IsAny<JToken>())).Throws(ex);
                    connection.Setup(c => c.OnError(It.IsAny<AggregateException>())).Callback<Exception>(e =>
                    {
                        Assert.Equal(ex, e.InnerException);
                        wh.Set();
                    });

                    var httpBasedTransport = new Mock<HttpBasedTransport>(httpClient.Object, "")
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

            mockWebSocket.SetupGet(ws => ws.State).Returns(state);
            mockConnection.Setup(c => c.OnError(It.IsAny<InvalidOperationException>()));

            var wsTransport = new WebSocketTransport();

            wsTransport.WebSocket = mockWebSocket.Object;

            var task = wsTransport.Send(mockConnection.Object, "", "");

            Assert.True(task.IsFaulted);
            Assert.IsType(typeof(InvalidOperationException), task.Exception.InnerException);

            mockConnection.VerifyAll();
            mockWebSocket.VerifyAll();
        }
    }
}
