using System;
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
            Assert.Equal(config.DisconnectTimeout.TotalSeconds, 30);
            Assert.Equal(config.KeepAlive, TimeSpan.FromTicks(config.DisconnectTimeout.Ticks/3));
            Assert.Equal(config.HeartbeatInterval(), TimeSpan.FromTicks(config.KeepAlive.Value.Ticks/2));
        }

        [Fact]
        public void DefaultKeepAliveThrowsWhenNegative()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(-1));
        }
    }
}
