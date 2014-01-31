using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Hubs
{
    public class TypedHubFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task CanInvokeMethodsAndReceiveMessagesFromValidTypedHub(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateHubConnection(host))
                {
                    var hub = connection.CreateHubProxy("ValidTypedHub");
                    var echoTcs = new TaskCompletionSource<string>();
                    var pingWh = new ManualResetEventSlim();

                    hub.On<string>("Echo", message => echoTcs.TrySetResult(message));
                    hub.On("Ping", pingWh.Set);

                    await connection.Start(host.TransportFactory());

                    hub.InvokeWithTimeout("Echo", "arbitrary message");
                    Assert.True(echoTcs.Task.Wait(TimeSpan.FromSeconds(10)));
                    Assert.Equal("arbitrary message", echoTcs.Task.Result);

                    hub.InvokeWithTimeout("Ping");
                    Assert.True(pingWh.Wait(TimeSpan.FromSeconds(10)));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task CannotInvokeMethodsAndReceiveMessagesFromInvalidTypedHub(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateHubConnection(host))
                {
                    var hub = connection.CreateHubProxy("InvalidTypedHub");
                    var tcs = new TaskCompletionSource<string>();

                    await connection.Start(host.TransportFactory());

                    var ex = Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("Echo", "arbitrary message"));
                    Assert.Equal(1, ex.InnerExceptions.Count);
                    Assert.IsType<InvalidOperationException>(ex.InnerExceptions[0]);

                    ex = Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("Ping"));
                    Assert.Equal(1, ex.InnerExceptions.Count);
                    Assert.IsType<InvalidOperationException>(ex.InnerExceptions[0]);
                }
            }
        }
    }
}
