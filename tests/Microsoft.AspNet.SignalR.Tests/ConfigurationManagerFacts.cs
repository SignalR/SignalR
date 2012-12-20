using Microsoft.AspNet.SignalR.Configuration;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ConfigurationManagerFacts
    {
        [Fact]
        public void DefaultValues()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Equal(config.ConnectionTimeout.TotalSeconds, 110);
            Assert.Equal(config.DisconnectTimeout.TotalSeconds, 40);
            Assert.Equal(config.HeartbeatInterval.TotalSeconds, 10);
            Assert.NotNull(config.KeepAlive);
            Assert.Equal(config.KeepAlive, 2);
        }
    }
}
