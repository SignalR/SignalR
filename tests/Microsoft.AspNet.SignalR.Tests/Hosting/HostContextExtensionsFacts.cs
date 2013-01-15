using System.Threading;
using Microsoft.AspNet.SignalR.Hosting;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Hosting
{
    public class HostContextExtensionsFacts
    {
        [Fact]
        public void ExtensionMethodsMapToDictionaryEntries()
        {
            // Arrange
            var request = new Mock<IRequest>();
            var response = new Mock<IResponse>();
            var context = new HostContext(request.Object, response.Object);

            // Act
            context.Items[HostConstants.DebugMode] = true;
            context.Items[HostConstants.InstanceName] = "Instance name";
            context.Items[HostConstants.ShutdownToken] = CancellationToken.None;
            context.Items[HostConstants.SupportsWebSockets] = false;
            context.Items[HostConstants.WebSocketServerUrl] = "ws://123";

            // Assert
            Assert.True(context.IsDebuggingEnabled());
            Assert.Equal("Instance name", context.InstanceName());
            Assert.Equal(CancellationToken.None, context.HostShutdownToken());
            Assert.False(context.SupportsWebSockets());
            Assert.Equal("ws://123", context.WebSocketServerUrl());
        }
    }
}
