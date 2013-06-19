using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubProxyFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        public void TransportTimesOutIfNoInitMessage(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            var mre = new ManualResetEventSlim();

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(transportConnectTimeout: 1, messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("DelayedOnConnectedHub");

                using (hubConnection)
                {
                    hubConnection.Start(host.Transport).Catch(ex =>
                    {
                        mre.Set();
                    });

                    Assert.True(mre.Wait(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Fact(Timeout = 5000)]
        public void WebSocketTransportDoesntHangIfConnectReturnsCancelledTask()
        {
            RunWebSocketTransportWithConnectTask(() =>
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetCanceled();
                return tcs.Task;
            });
        }

        [Fact(Timeout = 5000)]
        public void WebSocketTransportDoesntHangIfConnectReturnsFaultedTask()
        {
            RunWebSocketTransportWithConnectTask(() => TaskAsyncHelper.FromError(new Exception()));
        }

        public void RunWebSocketTransportWithConnectTask(Func<Task> taskReturn)
        {
            using (var host = CreateHost(HostType.IISExpress))
            {
                host.Initialize();

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("EchoHub");

                var transport = new Mock<WebSocketTransport>() { CallBase = true };
                transport.Setup(m => m.PerformConnect()).Returns(taskReturn());

                using (hubConnection)
                {
                    Assert.Throws<AggregateException>(() => hubConnection.Start(transport.Object).Wait());
                }
            }
        }

        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Auto, MessageBusType.Default)]
        public void ConnectionFailsStartOnMultipleTransportTimeouts(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            var mre = new ManualResetEventSlim();

            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(transportConnectTimeout: 1, messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("DelayedOnConnectedHub");

                using (hubConnection)
                {
                    hubConnection.Start(host.Transport).Catch(ex =>
                    {
                        mre.Set();
                    });

                    // Should take 1-2s per transport timeout
                    Assert.True(mre.Wait(TimeSpan.FromSeconds(15)));
                    Assert.True(String.IsNullOrEmpty(hubConnection.Transport.Name));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        public void InitMessageReceivedPriorToStartCompletion(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);
                // Does nothing on OnConnected so we shouldn't get any user generated messages
                IHubProxy proxy = hubConnection.CreateHubProxy("EchoHub");

                using (hubConnection)
                {
                    Assert.True(String.IsNullOrEmpty(hubConnection.MessageId));

                    hubConnection.Start(host.Transport).Wait();

                    // If we have an init message that means we got a message prior to start
                    Assert.False(String.IsNullOrEmpty(hubConnection.MessageId));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        public void TransportsBufferMessagesCorrectly(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("OnConnectedBufferHub");
                var bufferCountdown = new OrderedCountDownRange<int>(new[] { 0, 1 });
                int bufferMeCalls = 0;

                using (hubConnection)
                {
                    var wh = new ManualResetEvent(false);

                    proxy.On("pong", () =>
                    {
                        Assert.Equal(2, bufferMeCalls);

                        wh.Set();
                    });

                    proxy.On<int>("bufferMe", (val) =>
                    {
                        // Ensure correct ordering of the buffered messages
                        Assert.True(bufferCountdown.Expect(val));
                        bufferMeCalls++;
                        Assert.Equal(hubConnection.State, ConnectionState.Connected);
                    });

                    hubConnection.Start(host.Transport).Wait();

                    Assert.Equal(2, bufferMeCalls);

                    proxy.Invoke("Ping");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        public void TransportCanJoinGroupInOnConnected(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("GroupJoiningHub");
                int pingCount = 0;

                using (hubConnection)
                {
                    var wh = new ManualResetEvent(false);

                    proxy.On("ping", () =>
                    {
                        if (++pingCount == 2)
                        {
                            wh.Set();
                        }

                        Assert.True(pingCount <= 2);
                    });

                    hubConnection.Start(host.Transport).Wait();

                    proxy.Invoke("PingGroup");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        public void EndToEndTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("ChatHub");

                using (hubConnection)
                {
                    var wh = new ManualResetEvent(false);

                    proxy.On("addMessage", data =>
                    {
                        Assert.Equal("hello", data);
                        wh.Set();
                    });

                    hubConnection.Start(host.Transport).Wait();

                    proxy.InvokeWithTimeout("Send", "hello");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public void HubNamesAreNotCaseSensitive(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    IHubProxy proxy = hubConnection.CreateHubProxy("chatHub");
                    var wh = new ManualResetEvent(false);

                    proxy.On("addMessage", data =>
                    {
                        Assert.Equal("hello", data);
                        wh.Set();
                    });

                    hubConnection.Start(host.Transport).Wait();

                    proxy.InvokeWithTimeout("Send", "hello");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public void UnableToCreateHubThrowsError(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    IHubProxy proxy = hubConnection.CreateHubProxy("MyHub2");

                    hubConnection.Start(host.Transport).Wait();
                    var ex = Assert.Throws<AggregateException>(() => proxy.InvokeWithTimeout("Send", "hello"));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        public void ConnectionErrorCapturesExceptionsThrownInClientHubMethod(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                var wh = new ManualResetEventSlim();
                Exception thrown = new Exception(),
                          caught = null;

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
                        caught = e;
                        wh.Set();
                    };

                    connection.Start(host.Transport).Wait();
                    proxy.Invoke("Send", "");

                    Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
                    Assert.Equal(thrown, caught);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        public void RequestHeadersSetCorrectly(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);

                using (hubConnection)
                {
                    IHubProxy proxy = hubConnection.CreateHubProxy("ExamineHeadersHub");
                    var tcs = new TaskCompletionSource<object>();

                    proxy.On("sendHeader", (headers) =>
                    {
                        Assert.Equal<string>("test-header", (string)headers.testHeader);
                        if (transportType != TransportType.Websockets)
                        {
                            Assert.Equal<string>("referer", (string)headers.refererHeader);
                        }
                        tcs.TrySetResult(null);
                    });

                    hubConnection.Error += e => tcs.TrySetException(e);

                    hubConnection.Headers.Add("test-header", "test-header");
                    if (transportType != TransportType.Websockets)
                    {
                        hubConnection.Headers.Add(System.Net.HttpRequestHeader.Referer.ToString(), "referer");
                    }

                    hubConnection.Start(host.Transport).Wait();
                    proxy.Invoke("Send", "Hello");

                    Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        public void RequestHeadersCannotBeSetOnceConnected(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                host.Initialize(messageBusType: messageBusType);
                HubConnection hubConnection = CreateHubConnection(host);


                using (hubConnection)
                {
                    IHubProxy proxy = hubConnection.CreateHubProxy("ExamineHeadersHub");

                    hubConnection.Start(host.Transport).Wait();

                    var ex = Assert.Throws<InvalidOperationException>(() => hubConnection.Headers.Add("test-header", "test-header"));
                    Assert.Equal("Request headers cannot be set after the connection has started.", ex.Message);
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ClientCallbackInvalidNumberOfArgumentsExceptionThrown(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection hubConnection = CreateHubConnection(host);
                var tcs = new TaskCompletionSource<Exception>();

                hubConnection.Error += (ex) =>
                {
                    tcs.TrySetResult(ex);
                };

                using (hubConnection)
                {
                    IHubProxy proxy = hubConnection.CreateHubProxy("ClientCallbackHub");

                    proxy.On<string, string>("twoArgsMethod", (arg1, arg2) => { });

                    hubConnection.Start(host.Transport).Wait();
                    proxy.Invoke("SendInvalidNumberOfArguments").Wait();

                    Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(5)));
                    Assert.IsType(typeof(InvalidOperationException), tcs.Task.Result);
                    Assert.Equal(((InvalidOperationException)tcs.Task.Result).Message, "A client callback for event twoArgsMethod with 1 argument(s) could not be found");
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ClientCallbackArgumentTypeMismatchExceptionThrown(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();
                HubConnection hubConnection = CreateHubConnection(host);
                var tcs = new TaskCompletionSource<Exception>();

                hubConnection.Error += (ex) =>
                {
                    tcs.TrySetResult(ex);
                };

                using (hubConnection)
                {
                    IHubProxy proxy = hubConnection.CreateHubProxy("ClientCallbackHub");

                    proxy.On<int>("foo", args => { });

                    hubConnection.Start(host.Transport).Wait();
                    proxy.Invoke("SendArgumentsTypeMismatch").Wait();

                    Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(5)));
                    Assert.IsType(typeof(InvalidOperationException), tcs.Task.Result);
                    Assert.Equal(((InvalidOperationException)tcs.Task.Result).Message, "A client callback for event foo with 1 argument(s) was found, however an error occurred because Could not convert string to integer: arg1. Path ''.");
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
