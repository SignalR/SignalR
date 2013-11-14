using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;
using Moq;
using Microsoft.AspNet.SignalR.Transports;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.Tests.Transports
{
    public class TransportHeartBeatFacts
    {
        [Theory]
        // InlineData(configAllowTrackingOverride, connectionSkipTracking, shouldReturnNull)
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void AddOrUpdateReturnsNullForNoTrackRequests(bool configAllowTrackingOverride, bool connectionSkipTracking, bool shouldReturnNull)
        {
            // Arrange
            var connectionId = Guid.NewGuid().ToString();
            var existingConnection = new Mock<ITrackingConnection>();
            var noTrackConnection = new Mock<ITrackingConnection>();
            var configManager = new DefaultConfigurationManager();
            var serverCommandHandler = new TestServerCommandHandler();
            var serverIdManager = new Mock<IServerIdManager>();
            var perfCounterManager = new Mock<IPerformanceCounterManager>();
            var connectionsCurrentCounter = new Mock<IPerformanceCounter>();
            var traceManager = new Mock<ITraceManager>();
            var resolver = new Mock<IDependencyResolver>();
            existingConnection.SetupGet(c => c.ConnectionId).Returns(connectionId);
            existingConnection.SetupGet(c => c.SkipTracking).Returns(false);
            noTrackConnection.SetupGet(c => c.ConnectionId).Returns(connectionId);
            noTrackConnection.SetupGet(c => c.SkipTracking).Returns(connectionSkipTracking);
            configManager.AllowConnectionTrackingOverride = configAllowTrackingOverride;
            traceManager.Setup(t => t[It.IsAny<string>()]).Returns<string>(name => new TraceSource(name));
            perfCounterManager.SetupGet(p => p.ConnectionsCurrent).Returns(connectionsCurrentCounter.Object);
            connectionsCurrentCounter.SetupAllProperties();
            resolver.Setup(m => m.GetService(typeof(IConfigurationManager)))
                    .Returns(configManager);
            resolver.Setup(m => m.GetService(typeof(IServerCommandHandler)))
                    .Returns(serverCommandHandler);
            resolver.Setup(m => m.GetService(typeof(IServerIdManager)))
                    .Returns(serverIdManager.Object);
            resolver.Setup(m => m.GetService(typeof(IPerformanceCounterManager)))
                    .Returns(perfCounterManager.Object);
            resolver.Setup(m => m.GetService(typeof(ITraceManager)))
                    .Returns(traceManager.Object);
            var transportHeartbeat = new TransportHeartbeat(resolver.Object);
            transportHeartbeat.AddOrUpdateConnection(existingConnection.Object);

            // Act
            var result = transportHeartbeat.AddOrUpdateConnection(noTrackConnection.Object);
            
            // Assert
            if (shouldReturnNull)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.NotNull(result);
            }
        }

        internal class TestServerCommandHandler : IServerCommandHandler
        {
            public Task SendCommand(ServerCommand command)
            {
                return Task.FromResult(0);
            }

            public Action<ServerCommand> Command
            {
                get;
                set;
            }
        }
    }
}
