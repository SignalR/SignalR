// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubProxyFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task TransportTimesOutIfNoInitMessage(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            var mre = new TaskCompletionSource<object>();

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(transportConnectTimeout: 1, messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host, "/no-init");

                var proxy = hubConnection.CreateHubProxy("DelayedOnConnectedHub");

                using (hubConnection)
                {
                    await Assert.ThrowsAsync<HttpClientException>(() => hubConnection.Start(host.Transport)).OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Fact]
        public async Task WebSocketTransportDoesntHangIfConnectReturnsCancelledTask()
        {
            await RunWebSocketTransportWithConnectTask(() =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }).OrTimeout(10000);
        }

        [Fact]
        public async Task WebSocketTransportDoesntHangIfConnectReturnsFaultedTask()
        {
            await RunWebSocketTransportWithConnectTask(
                () => TaskAsyncHelper.FromError(new InvalidOperationException())).OrTimeout(10000);
        }

        public async Task RunWebSocketTransportWithConnectTask(Func<Task> taskReturn)
        {
            using (var host = CreateHost(HostType.HttpListener))
            {
                host.Initialize();

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("EchoHub");

                var transport = new Mock<WebSocketTransport>() { CallBase = true };
                transport.Setup(m => m.PerformConnect(It.IsAny<CancellationToken>())).Returns(taskReturn());

                using (hubConnection)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await hubConnection.Start(transport.Object));
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Auto, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Auto, MessageBusType.Default)]
        public async Task ConnectionFailsStartOnMultipleTransportTimeouts(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            var mre = new TaskCompletionSource<object>();

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(transportConnectTimeout: 1, messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host, "/no-init");
                var proxy = hubConnection.CreateHubProxy("DelayedOnConnectedHub");

                using (hubConnection)
                {
                    await Assert.ThrowsAsync<HttpClientException>(() => hubConnection.Start(host.Transport));

                    var transport = hubConnection.Transport;

                    // Should take 1-2s per transport timeout
                    Assert.Null(transport);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task InitMessageReceivedPriorToStartCompletion(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                // Does nothing on OnConnected so we shouldn't get any user generated messages
                var proxy = hubConnection.CreateHubProxy("EchoHub");

                using (hubConnection)
                {
                    Assert.True(String.IsNullOrEmpty(hubConnection.MessageId));

                    await hubConnection.Start(host.Transport);

                    // If we have an init message that means we got a message prior to start
                    Assert.False(String.IsNullOrEmpty(hubConnection.MessageId));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task TransportsQueueIncomingMessagesCorrectly(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("OnConnectedBufferHub");
                var bufferCountdown = new OrderedCountDownRange<int>(new[] { 0, 1 });
                var bufferMeCalls = 0;

                using (hubConnection)
                {
                    var wh = new TaskCompletionSource<object>();

                    proxy.On<int>("bufferMe", (val) =>
                    {
                        // Ensure correct ordering of the incoming messages
                        Assert.True(bufferCountdown.Expect(val));
                        bufferMeCalls++;
                        Assert.Equal(hubConnection.State, ConnectionState.Connected);
                    });

                    proxy.On("pong", () =>
                    {
                        Assert.Equal(2, bufferMeCalls);

                        wh.TrySetResult(null);
                    });

                    await hubConnection.Start(host.Transport).OrTimeout();

                    // The calls should be complete once the start task returns
                    Assert.Equal(2, bufferMeCalls);

                    await proxy.Invoke("Ping").OrTimeout();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task TransportCanJoinGroupInOnConnected(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("GroupJoiningHub");
                var pingCount = 0;

                using (hubConnection)
                {
                    var wh = new TaskCompletionSource<object>();

                    proxy.On("ping", () =>
                    {
                        if (++pingCount == 2)
                        {
                            wh.TrySetResult(null);
                        }

                        Assert.True(pingCount <= 2);
                    });

                    await hubConnection.Start(host.Transport);

                    var ignore = proxy.Invoke("PingGroup").Catch();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task EndToEndTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("ChatHub");

                using (hubConnection)
                {
                    var wh = new TaskCompletionSource<object>();

                    proxy.On("addMessage", data =>
                    {
                        Assert.Equal("hello", data);
                        wh.TrySetResult(null);
                    });

                    await hubConnection.Start(host.Transport);

                    await proxy.Invoke("Send", "hello").OrTimeout();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubNamesAreNotCaseSensitive(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("chatHub");
                    var wh = new TaskCompletionSource<object>();

                    proxy.On("addMessage", data =>
                    {
                        Assert.Equal("hello", data);
                        wh.TrySetResult(null);
                    });

                    await hubConnection.Start(host.Transport);

                    await proxy.Invoke("Send", "hello").OrTimeout();

                    await wh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task UnableToCreateHubThrowsError(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("MyHub2");

                    await hubConnection.Start(host.Transport);
                    await Assert.ThrowsAnyAsync<Exception>(() => proxy.Invoke("Send", "hello").OrTimeout());
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task ConnectionErrorCapturesExceptionsThrownInClientHubMethod(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                var wh = new TaskCompletionSource<Exception>();
                var thrown = new Exception();

                host.Initialize(messageBusType: messageBusType);

                var connection = CreateHubConnection(host);

                using (connection)
                {
                    var proxy = connection.CreateHubProxy("ChatHub");

                    proxy.On<string>("addMessage", (message) =>
                    {
                        throw thrown;
                    });

                    connection.Error += e =>
                    {
                        wh.TrySetResult(e);
                    };

                    await connection.Start(host.Transport);
                    var ignore = proxy.Invoke("Send", "").Catch();

                    Assert.Equal(thrown, await wh.Task.OrTimeout());
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        public async Task RequestHeadersSetCorrectly(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("ExamineHeadersHub");
                    var tcs = new TaskCompletionSource<object>();

                    proxy.On("sendHeader", headers =>
                    {
                        Assert.Equal("test-header", (string)headers.testHeader);
                        if (transportType != TransportType.Websockets)
                        {
                            Assert.Equal("referer", (string)headers.refererHeader);
                        }
                        tcs.TrySetResult(null);
                    });

                    hubConnection.Error += e => tcs.TrySetException(e);

                    hubConnection.Headers.Add("test-header", "test-header");
                    if (transportType != TransportType.Websockets)
                    {
                        hubConnection.Headers.Add(System.Net.HttpRequestHeader.Referer.ToString(), "referer");
                    }

                    await hubConnection.Start(host.Transport);
                    var ignore = proxy.Invoke("Send").Catch();

                    await tcs.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task RequestHeadersCanBeSetOnceConnected(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                host.Initialize(messageBusType: messageBusType);
                var hubConnection = CreateHubConnection(host);
                var mre = new TaskCompletionSource<object>();

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("ExamineHeadersHub");

                    proxy.On("sendHeader", headers =>
                    {
                        Assert.Equal("test-header", (string)headers.testHeader);
                        mre.TrySetResult(null);
                    });

                    await hubConnection.Start(host.Transport);

                    hubConnection.Headers.Add("test-header", "test-header");
                    var ignore = proxy.Invoke("Send").Catch();
                    await mre.Task.OrTimeout();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        //[InlineData(HostType.Memory, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ClientCallbackInvalidNumberOfArgumentsExceptionThrown(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var hubConnection = CreateHubConnection(host);
                var tcs = new TaskCompletionSource<Exception>();

                hubConnection.Error += (ex) =>
                {
                    tcs.TrySetException(ex);
                };

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("ClientCallbackHub");

                    proxy.On<string, string>("twoArgsMethod", (arg1, arg2) => { });

                    await hubConnection.Start(host.Transport).OrTimeout();
                    await proxy.Invoke("SendOneArgument").OrTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tcs.Task);
                    Assert.Equal(ex.Message, "A client callback for event twoArgsMethod with 1 argument(s) could not be found");
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        //[InlineData(HostType.Memory, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ClientCallbackWithFewerArgumentsDoesNotThrow(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var hubConnection = CreateHubConnection(host);
                var mre = new TaskCompletionSource<object>();
                var wh = new TaskCompletionSource<object>();

                hubConnection.Error += (ex) =>
                {
                    wh.TrySetResult(null);
                };

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("ClientCallbackHub");

                    proxy.On("twoArgsMethod", () => { mre.TrySetResult(null); });

                    await hubConnection.Start(host.Transport);
                    await proxy.Invoke("SendOneArgument");

                    await mre.Task.OrTimeout();
                    Assert.False(wh.Task.IsCompleted);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        //[InlineData(HostType.Memory, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task ClientCallbackArgumentTypeMismatchExceptionThrown(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var hubConnection = CreateHubConnection(host);
                var tcs = new TaskCompletionSource<Exception>();

                hubConnection.Error += (ex) =>
                {
                    tcs.TrySetResult(ex);
                };

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("ClientCallbackHub");

                    proxy.On<int>("foo", args => { });

                    await hubConnection.Start(host.Transport);
                    await proxy.Invoke("SendArgumentsTypeMismatch");

                    var thrown = Assert.IsType<InvalidOperationException>(await tcs.Task.OrTimeout());
                    Assert.Equal(thrown.Message, "A client callback for event foo with 1 argument(s) was found, however an error occurred because Could not convert string to integer: arg1. Path ''.");
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        //[InlineData(HostType.Memory, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        public async Task WaitingOnHubInvocationDoesNotDeadlock(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var hubConnection = CreateHubConnection(host);
                var mre = new TaskCompletionSource<object>();

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("EchoHub");
                    var callbackInvokedCount = 0;

                    proxy.On<string>("echo", message =>
                    {
                        callbackInvokedCount++;
                        if (callbackInvokedCount == 4)
                        {
                            mre.TrySetResult(null);
                        }
                        else
                        {
                            proxy.Invoke("EchoCallback", message);
                        }
                    });

                    await hubConnection.Start(host.Transport);
                    var ignore = proxy.Invoke("EchoCallback", "message").Catch();
                    await mre.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling)]
        public async Task CallingStopAfterAwaitingInvocationReturnsFast(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("EchoHub");

                    await hubConnection.Start(host.Transport).OrTimeout();

                    await proxy.Invoke("EchoCallback", "message").OrTimeout();
                }
            }
        }

        [Theory]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling)]
        public async Task CallingStopInClientMethodWorks(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("EchoHub");

                    proxy.On<string>("echo", message =>
                    {
                        hubConnection.Stop();
                    });

                    await hubConnection.Start(host.Transport).OrTimeout();

                    try
                    {
                        await proxy.Invoke("EchoAndDelayCallback", "message").OrTimeout();
                        Assert.True(false, "The hub method invocation should fail.");
                    }
                    catch (InvalidOperationException)
                    {
                        // This should throw as the invocation result will not be received due to the connection stopping
                        Assert.True(true);
                    }
                }
            }
        }

        [Theory]
        [InlineData(HostType.HttpListener, TransportType.Websockets)]
        public async Task NoDeadlockWhenBlockingAfterInvokingProxyMethod(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                var hubConnection = CreateHubConnection(host);
                var mre = new TaskCompletionSource<object>();

                using (hubConnection)
                {
                    var proxy = hubConnection.CreateHubProxy("EchoHub");

                    var called = false;
                    proxy.On<string>("echo", message =>
                    {
                        if (!called)
                        {
                            called = true;
                            proxy.Invoke("EchoCallback", "message");
                        }
                        else
                        {
                            mre.TrySetResult(null);
                        }
                    });

                    await hubConnection.Start(host.Transport).OrTimeout();
                    await proxy.Invoke("EchoCallback", "message").OrTimeout();

                    await mre.Task.OrTimeout();

                    hubConnection.Stop();
                }
            }
        }

        public class MyHub2 : Hub
        {
            public MyHub2(int n)
            {

            }

            public void Send(string value)
            {

            }
        }

        public class ChatHub : Hub
        {
            public Task Send(string message)
            {
                return Clients.All.addMessage(message);
            }
        }
    }
}
