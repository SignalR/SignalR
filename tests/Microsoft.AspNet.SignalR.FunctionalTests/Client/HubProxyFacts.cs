﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubProxyFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void EndToEndTest(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("ChatHub");
                var wh = new ManualResetEvent(false);

                proxy.On("addMessage", data =>
                {
                    Assert.Equal("hello", data);
                    wh.Set();
                });

                hubConnection.Start(host.Transport).Wait();

                proxy.InvokeWithTimeout("Send", "hello");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(10)));

                hubConnection.Stop();
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        public void HubNamesAreNotCaseSensitive(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection hubConnection = CreateHubConnection(host);
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

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        public void UnableToCreateHubThrowsError(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("MyHub2");

                hubConnection.Start(host.Transport).Wait();
                var ex = Assert.Throws<AggregateException>(() => proxy.InvokeWithTimeout("Send", "hello"));
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        public void ConnectionErrorCapturesExceptionsThrownInClientHubMethod(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                var wh = new ManualResetEventSlim();
                Exception thrown = new Exception(),
                          caught = null;

                host.Initialize();

                var connection = CreateHubConnection(host);
                var proxy = connection.CreateHubProxy("ChatHub");

                proxy.On("addMessage", () =>
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
