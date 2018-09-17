// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Infrastructure;
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
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        public async Task CanInvokeMethodsAndReceiveMessagesFromValidTypedHub(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                using (var connection = CreateHubConnection(host))
                {
                    var hub = connection.CreateHubProxy("ValidTypedHub");
                    var echoTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var pingWh = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                    hub.On<string>("Echo", message => echoTcs.TrySetResult(message));
                    hub.On("Ping", () => pingWh.TrySetResult(null));

                    await connection.Start(host.TransportFactory());

                    await hub.Invoke("Echo", "arbitrary message").OrTimeout();
                    Assert.Equal("arbitrary message", await echoTcs.Task.OrTimeout(TimeSpan.FromSeconds(10)));

                    await hub.Invoke("Ping").OrTimeout();
                    await pingWh.Task.OrTimeout(TimeSpan.FromSeconds(10));
                }
            }
        }

        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.Fake)]
        //[InlineData(HostType.Memory, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
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

                    await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("Echo", "arbitrary message").OrTimeout());
                    await Assert.ThrowsAsync<InvalidOperationException>(() => hub.Invoke("Ping").OrTimeout());
                }
            }
        }
    }
}
