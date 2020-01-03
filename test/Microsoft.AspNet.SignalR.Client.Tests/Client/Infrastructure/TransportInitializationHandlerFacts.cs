// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class TransportInitializationHandlerFacts
    {
        [Fact]
        public async Task StartRequestNotCalledIfTransportAlreadyFailed()
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(1));

            var mockTransportHelper = new Mock<TransportHelper>();
            var failureInvokedTcs = new TaskCompletionSource<object>();

            var initHandler = new TransportInitializationHandler(Mock.Of<IHttpClient>(), mockConnection.Object,
                string.Empty, "fakeTransport", CancellationToken.None, mockTransportHelper.Object);

            initHandler.OnFailure += () => failureInvokedTcs.TrySetResult(null);

            initHandler.TryFailStart();

            await failureInvokedTcs.Task.OrTimeout();

            initHandler.InitReceived();

            mockTransportHelper.Verify(
                h => h.GetStartResponse(It.IsAny<IHttpClient>(), It.IsAny<IConnection>(),
                    It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public async Task InitTaskIsFailedIfFailureOccursAfterStartRequestStarted()
        {
            var mockTransportHelper = new Mock<TransportHelper>();
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(p => p.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(0.5));
            mockConnection.Setup(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());

            var initHandler = new TransportInitializationHandler(Mock.Of<IHttpClient>(), mockConnection.Object,
                string.Empty, "fakeTransport", CancellationToken.None, mockTransportHelper.Object);

            var onFailureInvoked = false;
            initHandler.OnFailure += () => onFailureInvoked = true;

            var exception = new Exception("test exception");

            mockTransportHelper.Setup(
                h => h.GetStartResponse(It.IsAny<IHttpClient>(), It.IsAny<IConnection>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .Returns<IHttpClient, IConnection, string, string>(
                    (httpClient, connection, connectionData, transport) =>
                    {
                        initHandler.TryFailStart(exception);
                        return Task.FromResult("{ \"Response\" : \"started\" }");
                    });

            initHandler.InitReceived();

            var startException = await Assert.ThrowsAsync<StartException>(() => initHandler.Task);

            Assert.Same(exception, startException.InnerException);

            Assert.True(onFailureInvoked);
        }

        [Fact]
        public async Task TimeoutDoesNotFailTheTaskAfterInitReceived()
        {
            var mockTransportHelper = new Mock<TransportHelper>();
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(p => p.TotalTransportConnectTimeout).Returns(TimeSpan.FromMilliseconds(200));
            mockConnection.Setup(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());

            mockTransportHelper.Setup(
                h => h.GetStartResponse(It.IsAny<IHttpClient>(), It.IsAny<IConnection>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .Returns<IHttpClient, IConnection, string, string>(
                    (httpClient, connection, connectionData, transport) =>
                        // wait for the timeout to fire
                        Task.Delay(250).ContinueWith(t => "{ \"Response\" : \"started\" }"));

            var initHandler = new TransportInitializationHandler(Mock.Of<IHttpClient>(), mockConnection.Object,
                string.Empty, "fakeTransport", CancellationToken.None, mockTransportHelper.Object);
            var onFailureInvoked = false;
            initHandler.OnFailure += () => onFailureInvoked = true;

            initHandler.InitReceived();

            await initHandler.Task;
            Assert.False(onFailureInvoked);
        }

        [Fact]
        public async Task InitTaskThrowsStartFailedExceptionIfStartRequestThrows()
        {
            var mockTransportHelper = new Mock<TransportHelper>();
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(p => p.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(5));
            mockConnection.Setup(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());

            var initHandler = new TransportInitializationHandler(Mock.Of<IHttpClient>(), mockConnection.Object,
                string.Empty, "fakeTransport", CancellationToken.None, mockTransportHelper.Object);

            var onFailureInvoked = false;
            initHandler.OnFailure += () => onFailureInvoked = true;

            var exception = new Exception("test exception");

            mockTransportHelper.Setup(
                h => h.GetStartResponse(It.IsAny<IHttpClient>(), It.IsAny<IConnection>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .Returns<IHttpClient, IConnection, string, string>(
                    (httpClient, connection, connectionData, transport) =>
                    {
                        var tcs = new TaskCompletionSource<string>();
                        tcs.SetException(exception);
                        return tcs.Task;
                    });

            initHandler.InitReceived();

            var startException = await Assert.ThrowsAsync<StartException>(async () => await initHandler.Task);

            // startException.InnerException is an AggregateException
            Assert.Same(exception, startException.InnerException.InnerException);
            Assert.True(onFailureInvoked);
        }

        [Fact]
        public async Task InitTaskThrowsStartFailedExceptionIfStartRequestReturnsIncorrectResult()
        {
            var mockTransportHelper = new Mock<TransportHelper>();
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(p => p.TotalTransportConnectTimeout).Returns(TimeSpan.FromSeconds(5));
            mockConnection.Setup(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());

            var initHandler = new TransportInitializationHandler(Mock.Of<IHttpClient>(), mockConnection.Object,
                string.Empty, "fakeTransport", CancellationToken.None, mockTransportHelper.Object);

            var onFailureInvoked = false;
            initHandler.OnFailure += () => onFailureInvoked = true;

            mockTransportHelper.Setup(
                h => h.GetStartResponse(It.IsAny<IHttpClient>(), It.IsAny<IConnection>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .Returns<IHttpClient, IConnection, string, string>(
                    (httpClient, connection, connectionData, transport)
                        => Task.FromResult("{ \"foo\" : \"bar\" }"));

            initHandler.InitReceived();

            await Assert.ThrowsAsync<StartException>(() => initHandler.Task);

            Assert.True(onFailureInvoked);
        }

        [Fact]
        public async Task FailIsNoOpAfterStartCompletedSuccessfully()
        {
            var mockTransportHelper = new Mock<TransportHelper>();
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(p => p.TotalTransportConnectTimeout).Returns(TimeSpan.FromMilliseconds(200));
            mockConnection.Setup(c => c.JsonSerializer).Returns(JsonSerializer.CreateDefault());

            var initHandler = new TransportInitializationHandler(Mock.Of<IHttpClient>(), mockConnection.Object,
                string.Empty, "fakeTransport", CancellationToken.None, mockTransportHelper.Object);

            var onFailureInvoked = false;
            initHandler.OnFailure += () => onFailureInvoked = true;

            mockTransportHelper.Setup(
                h => h.GetStartResponse(It.IsAny<IHttpClient>(), It.IsAny<IConnection>(),
                    It.IsAny<string>(), It.IsAny<string>()))
                .Returns<IHttpClient, IConnection, string, string>(
                    (httpClient, connection, connectionData, transport) => Task.FromResult("{ \"Response\" : \"started\" }"));

            initHandler.InitReceived();
            initHandler.TryFailStart();

            await initHandler.Task;
            Assert.False(onFailureInvoked);
        }

        [Fact]
        public async Task FailInvokedIfDisconnectTokenTripped()
        {
            var mockTransportHelper = new Mock<TransportHelper>();
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(p => p.TotalTransportConnectTimeout).Returns(TimeSpan.FromMilliseconds(200));

            var cancellationTokenSource = new CancellationTokenSource();

            var initHandler = new TransportInitializationHandler(Mock.Of<IHttpClient>(), mockConnection.Object,
                string.Empty, "fakeTransport", cancellationTokenSource.Token, mockTransportHelper.Object);

            var onFailureInvoked = false;
            initHandler.OnFailure += () => onFailureInvoked = true;

            cancellationTokenSource.Cancel();

            var exception = await Assert.ThrowsAsync<OperationCanceledException>(() => initHandler.Task);

            Assert.Equal(Resources.Error_ConnectionCancelled, exception.Message);
            Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);

            Assert.True(onFailureInvoked);
        }
    }
}
