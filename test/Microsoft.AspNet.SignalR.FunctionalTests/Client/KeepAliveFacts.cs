// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class KeepAliveFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task ReconnectionSuccesfulTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new TaskCompletionSource<object>();
                host.Initialize(keepAlive: null, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromSeconds(2));

                    connection.Reconnected += () =>
                    {
                        mre.TrySetResult(null);
                    };

                    await connection.Start(host.Transport);

                    await mre.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }


        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task SuccessiveTimeoutTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var mre = new AsyncManualResetEvent(false);
                host.Initialize(keepAlive: 5, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    connection.Reconnected += () =>
                    {
                        mre.Set();
                    };

                    await connection.Start(host.Transport);

                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromMilliseconds(300));

                    // Assert that Reconnected is called
                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(15)));

                    // Assert that Reconnected is called again
                    mre.Reset();
                    Assert.True(await mre.WaitAsync(TimeSpan.FromSeconds(15)));

                    // Clean-up
                    mre.Dispose();
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        public async Task OnConnectionSlowTest(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                // Arrange
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                host.Initialize(keepAlive: null, messageBusType: messageBusType);
                var connection = CreateConnection(host, "/my-reconnect");

                using (connection)
                {
                    ((Client.IConnection)connection).KeepAliveData = new KeepAliveData(TimeSpan.FromSeconds(21), TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5));

                    connection.ConnectionSlow += () =>
                    {
                        tcs.TrySetResult(null);
                    };

                    await connection.Start(host.Transport);

                    // Assert
                    await tcs.Task.OrTimeout(TimeSpan.FromSeconds(15));
                }
            }
        }
    }
}
