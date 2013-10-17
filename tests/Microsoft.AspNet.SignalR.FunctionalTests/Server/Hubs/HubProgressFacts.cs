using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Hubs
{
    public class HubProgressFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.IISExpress, TransportType.Websockets, MessageBusType.Default)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.Websockets, MessageBusType.FakeMultiStream)]
        [InlineData(HostType.HttpListener, TransportType.ServerSentEvents, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.Default)]
        [InlineData(HostType.HttpListener, TransportType.LongPolling, MessageBusType.FakeMultiStream)]
        public async Task HubProgressIsReportedSuccessfully(HostType hostType, TransportType transportType, MessageBusType messageBusType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize(messageBusType: messageBusType);

                HubConnection hubConnection = CreateHubConnection(host);
                IHubProxy proxy = hubConnection.CreateHubProxy("progress");
                var progressUpdates = new List<int>();
                var jobName = "test";

                using (hubConnection)
                {    
                    await hubConnection.Start(host.Transport);

                    var result = await proxy.Invoke<string, int>("DoLongRunningJob", progress => progressUpdates.Add(progress), jobName);

                    Assert.Equal(new[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, progressUpdates);
                    Assert.Equal(String.Format("{0} done!", jobName), result);
                }
            }
        }
    }
}
