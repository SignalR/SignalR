// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class TransportFacts
    {
        [Fact]
        public async Task CancelledTaskHandledinServerSentEvents()
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

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => sse.Start(connection.Object, string.Empty, CancellationToken.None).OrTimeout());
        }

        [Fact]
        public async Task CancelledTaskHandledinAutoTransport()
        {
            var tcs = new TaskCompletionSource<IResponse>();

            tcs.TrySetCanceled();

            var transport = new Mock<IClientTransport>();
            transport.Setup(t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), CancellationToken.None))
                .Returns(tcs.Task);

            var transports = new List<IClientTransport>();
            transports.Add(transport.Object);

            var autoTransport = new AutoTransport(new DefaultHttpClient(), transports);

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => autoTransport.Start(new Connection("http://foo"), string.Empty, CancellationToken.None));
        }

        [Fact]
        public async Task StartExceptionStopsAutoTransportFallback()
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
            await Assert.ThrowsAsync<StartException>(
                () => autoTransport.Start(new Connection("http://foo"), string.Empty, CancellationToken.None));

            failingTransport.Verify();
            unusedTransport.Verify(t => t.Start(It.IsAny<IConnection>(), It.IsAny<string>(), CancellationToken.None), Times.Never());
        }

        [Fact]
        public async Task CancelledTaskHandledWhenStartingLongPolling()
        {
            var tcs = new TaskCompletionSource<IResponse>();
            tcs.TrySetCanceled();

            var httpClient = new Mock<IHttpClient>();

            httpClient.Setup(c => c.Post(It.IsAny<string>(),
                It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(tcs.Task);

            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(15));

            var longPollingTransport = new LongPollingTransport(httpClient.Object);

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => longPollingTransport.Start(mockConnection.Object, null, CancellationToken.None).OrTimeout());
        }

        [Fact]
        public async Task CancelledTaskHandledinLongPollingLoop()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(1500));

            var tcs = new TaskCompletionSource<IResponse>();
            tcs.TrySetCanceled();

            var onErrorWh = new TaskCompletionSource<object>();

            var mockHttpClient = new Mock<IHttpClient>();
            mockHttpClient.Setup(c => c.Post(It.IsAny<string>(),
                It.IsAny<Action<IRequest>>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>()))
                .Returns(() => tcs.Task);

            var mockLongPollingTransport = new Mock<LongPollingTransport>(mockHttpClient.Object) { CallBase = true };
            mockLongPollingTransport.Setup(t => t.OnError(It.IsAny<IConnection>(), It.IsAny<Exception>()))
                .Callback(() => onErrorWh.TrySetResult(null));

            mockLongPollingTransport.Object.StartPolling(mockConnection.Object, string.Empty);

            await onErrorWh.Task.OrTimeout();

            // If the test is running on a slower machine or with a lot of other parallel threads,
            // it could take longer than 2 seconds, which means the transport will poll again and get the error
            // multiple times, so we use Times.AtLeastOnce to make the test more resiliant.
            mockLongPollingTransport
                .Verify(
                    t => t.OnError(It.IsAny<IConnection>(),
                            It.Is<OperationCanceledException>(e => string.Equals(e.Message, Resources.Error_TaskCancelledException))),
                    Times.AtLeastOnce());
        }

        [Fact]
        public async Task WebSocketsTriedByAutoTransportIfTryWebSocketsIsSetInLastNegotiateResponse()
        {
            var fallbackTransport = new Mock<IClientTransport>();
            var webSocketTransport = new Mock<IClientTransport>();
            webSocketTransport.Setup(m => m.Name).Returns(new WebSocketTransport().Name);

            var transports = new List<IClientTransport>()
            {
                webSocketTransport.Object,
                fallbackTransport.Object,
            };

            var autoTransport = new Mock<AutoTransport>(null, transports);
            autoTransport
                .Setup(c => c.GetNegotiateResponse(null, string.Empty))
                .Returns(Task.FromResult(new NegotiationResponse { TryWebSockets = false }));

            await autoTransport.Object.Negotiate(null, string.Empty).OrTimeout();

            autoTransport
                .Setup(c => c.GetNegotiateResponse(null, string.Empty))
                .Returns(Task.FromResult(new NegotiationResponse { TryWebSockets = true }));

            await autoTransport.Object.Negotiate(null, string.Empty).OrTimeout();

            await autoTransport.Object.Start(null, string.Empty, CancellationToken.None).OrTimeout();

            webSocketTransport.Verify(m => m.Start(null, string.Empty, CancellationToken.None), Times.Once());
            fallbackTransport.Verify(m => m.Start(null, string.Empty, CancellationToken.None), Times.Never());
        }

        [Fact]
        public async Task WebSocketsNotTriedByAutoTransportIfTryWebSocketsIsNotSetInLastNegotiateResponse()
        {
            var fallbackTransport = new Mock<IClientTransport>();
            var webSocketTransport = new Mock<IClientTransport>();
            webSocketTransport.Setup(m => m.Name).Returns(new WebSocketTransport().Name);

            var transports = new List<IClientTransport>()
            {
                webSocketTransport.Object,
                fallbackTransport.Object,
            };

            var autoTransport = new Mock<AutoTransport>(null, transports);
            autoTransport
                .Setup(c => c.GetNegotiateResponse(null, string.Empty))
                .Returns(Task.FromResult(new NegotiationResponse { TryWebSockets = true }));

            await autoTransport.Object.Negotiate(null, string.Empty).OrTimeout();

            autoTransport
                .Setup(c => c.GetNegotiateResponse(null, string.Empty))
                .Returns(Task.FromResult(new NegotiationResponse { TryWebSockets = false }));

            await autoTransport.Object.Negotiate(null, string.Empty).OrTimeout();

            await autoTransport.Object.Start(null, string.Empty, CancellationToken.None).OrTimeout();

            webSocketTransport.Verify(m => m.Start(null, string.Empty, CancellationToken.None), Times.Never());
            fallbackTransport.Verify(m => m.Start(null, string.Empty, CancellationToken.None), Times.Once());
        }

        [Fact]
        public async Task AutoTransportStartThrowsIfNoCompatibleTransportFound()
        {
            var webSocketTransport = new Mock<IClientTransport>();
            webSocketTransport.Setup(m => m.Name).Returns(new WebSocketTransport().Name);

            var transports = new List<IClientTransport>()
            {
                webSocketTransport.Object,
            };

            var autoTransport = new Mock<AutoTransport>(null, transports);
            autoTransport
                .Setup(c => c.GetNegotiateResponse(null, string.Empty))
                .Returns(Task.FromResult(new NegotiationResponse { TryWebSockets = false }));

            await autoTransport.Object.Negotiate(null, string.Empty).OrTimeout();

            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await autoTransport.Object.Start(null, string.Empty, CancellationToken.None)).OrTimeout();

            Assert.Equal(Resources.Error_NoCompatibleTransportFound, ex.Message);
            webSocketTransport.Verify(m => m.Start(null, string.Empty, CancellationToken.None), Times.Never());
        }

        [Fact]
        public async Task WebSocketsTriedByAutoTransportIfNegotiateIsNotCalled()
        {
            var fallbackTransport = new Mock<IClientTransport>();
            var webSocketTransport = new Mock<IClientTransport>();
            webSocketTransport.Setup(m => m.Name).Returns(new WebSocketTransport().Name);

            var transports = new List<IClientTransport>()
            {
                webSocketTransport.Object,
                fallbackTransport.Object,
            };

            var autoTransport = new AutoTransport(null, transports);

            await autoTransport.Start(null, string.Empty, CancellationToken.None).OrTimeout();

            webSocketTransport.Verify(m => m.Start(null, string.Empty, CancellationToken.None), Times.Once());
            fallbackTransport.Verify(m => m.Start(null, string.Empty, CancellationToken.None), Times.Never());
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
        [InlineData(InternalWebSocketState.Aborted)]
        [InlineData(InternalWebSocketState.Closed)]
        [InlineData(InternalWebSocketState.CloseReceived)]
        [InlineData(InternalWebSocketState.CloseSent)]
        [InlineData(InternalWebSocketState.Connecting)]
        public void WebSocketSendReturnsAFaultedTaskWhenNotConnected(InternalWebSocketState state)
        {
            var mockConnection = new Mock<Client.IConnection>(MockBehavior.Strict);
            var mockWebSocket = new Mock<WebSocket>(MockBehavior.Strict);
            var mockWebSocketHandler = new Mock<ClientWebSocketHandler>();

            mockWebSocket.SetupGet(ws => ws.State).Returns((WebSocketState)state);
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

        // Copy of WebSocketState because Xunit refuses to serialize WebSocketState because it's in a framework assembly
        public enum InternalWebSocketState
        {
            None = WebSocketState.None,
            Connecting = WebSocketState.Connecting,
            Open = WebSocketState.Open,
            CloseSent = WebSocketState.CloseSent,
            CloseReceived = WebSocketState.CloseReceived,
            Closed = WebSocketState.Closed,
            Aborted = WebSocketState.Aborted
        }
    }
}
