// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Hubs
{
    public class HubProgressFacts : HostedTest
    {
        [Theory(Skip = "Flaky on CI")]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubProgressIsReportedSuccessfully(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("progress");
                var progressUpdates = Channel.CreateUnbounded<int>();
                var jobName = "test";

                using (hubConnection)
                {
                    await hubConnection.Start(host.Transport);

                    var resultTask = proxy.Invoke<string, int>(
                        "DoLongRunningJob",
                        progress => Assert.True(progressUpdates.Writer.TryWrite(progress), "Channel should be unbounded!"),
                        jobName);

                    // Give up after 2 minutes
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(10));

                    var updatesSeen = 0;
                    try
                    {
                        while (updatesSeen < 10 && await progressUpdates.Reader.WaitToReadAsync(cts.Token))
                        {
                            while (updatesSeen < 10 && progressUpdates.Reader.TryRead(out var item))
                            {
                                var expected = updatesSeen * 10;
                                Assert.True(item == expected, $"Progress record {updatesSeen} was expected to be {expected} but was {item}");
                                updatesSeen += 1;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Assert.True(false, $"Timed out while waiting for all progress items to arrive. Received: {updatesSeen} items.");
                    }

                    Assert.Equal(10, updatesSeen);

                    // Wait for the invoke to complete (it should be done already...)
                    await resultTask.OrTimeout();
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubProgressThrowsInvalidOperationExceptionIfAttemptToReportProgressAfterMethodReturn(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("progress");

                proxy.On<bool>("sendProgressAfterMethodReturnResult", result => Assert.True(result));

                using (hubConnection)
                {
                    await hubConnection.Start(host.Transport);

                    await proxy.Invoke<int>("SendProgressAfterMethodReturn", _ => { });
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubProgressReportsProgressForInt(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("progress");

                using (hubConnection)
                {
                    await hubConnection.Start(host.Transport);

                    await proxy.Invoke<int>("ReportProgressInt", progress => Assert.Equal(100, progress));
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubProgressReportsProgressForString(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("progress");

                using (hubConnection)
                {
                    await hubConnection.Start(host.Transport);

                    await proxy.Invoke<string>("ReportProgressString", progress => Assert.Equal("Progress is 100%", progress));
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubProgressReportsProgressForCustomType(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("progress");

                using (hubConnection)
                {
                    await hubConnection.Start(host.Transport);

                    await proxy.Invoke<ProgressUpdate>("ReportProgressTyped", progress =>
                    {
                        Assert.Equal(100, progress.Percent);
                        Assert.Equal("Progress is 100%", progress.Message);
                    });
                }
            }
        }

        [Theory]
        //[InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.ServerSentEvents, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default, Skip = "Disabled IIS Express tests because they fail to initialize")]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        //[InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.FakeMultiStream)]
        //[InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubProgressReportsProgressForDynamic(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                var hubConnection = CreateHubConnection(host);
                var proxy = hubConnection.CreateHubProxy("progress");

                using (hubConnection)
                {
                    await hubConnection.Start(host.Transport);

                    await proxy.Invoke<dynamic>("ReportProgressDynamic", progress =>
                    {
                        Assert.Equal(100, (int)progress.Percent);
                        Assert.Equal("Progress is 100%", (string)progress.Message);
                    });
                }
            }
        }

        public class ProgressUpdate
        {
            public int Percent { get; set; }
            public string Message { get; set; }
        }
    }
}
