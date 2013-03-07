using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Newtonsoft.Json;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ConnectionEventFacts : HostedTest
    {
        [Theory]
        [InlineData(HostType.Memory, TransportType.ServerSentEvents)]
        [InlineData(HostType.Memory, TransportType.LongPolling)]
        [InlineData(HostType.IISExpress, TransportType.Websockets)]
        [InlineData(HostType.IISExpress, TransportType.ServerSentEvents)]
        [InlineData(HostType.IISExpress, TransportType.LongPolling)]
        public void NegotiatedEventTriggersCorrectly(HostType hostType, TransportType transportType)
        {
            using (var host = CreateHost(hostType, transportType))
            {
                host.Initialize();

                HubConnection hubConnection = CreateHubConnection(host);
                var triggered = false;
                
                hubConnection.Negotiated += (rawSettings) =>
                {
                    var result = JsonConvert.DeserializeObject<NegotiationResponse>(rawSettings);

                    Assert.NotNull(result);
                    Assert.NotNull(result.ConnectionId);
                    Assert.NotNull(result.ConnectionToken);
                    Assert.NotNull(result.Url);
                    Assert.NotNull(result.ProtocolVersion);
                    Assert.NotNull(result.DisconnectTimeout);
                    Assert.NotNull(result.TryWebSockets);
                    Assert.NotNull(result.KeepAliveTimeout);

                    triggered = true;
                };

                hubConnection.Start(host.Transport).Wait();

                Assert.True(triggered);

                hubConnection.Stop();
            }
        }
    }
}
