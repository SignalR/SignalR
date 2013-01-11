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
            Assert.Equal(110, config.ConnectionTimeout.TotalSeconds);
            Assert.Equal(30, config.DisconnectTimeout.TotalSeconds);
            Assert.Equal(10, config.KeepAlive.Value.TotalSeconds);
            Assert.Equal(20, config.KeepAliveTimeout().Value.TotalSeconds);
            Assert.Equal(5, config.HeartbeatInterval().TotalSeconds);
            Assert.Equal(100, config.TopicTtl().TotalSeconds);
        }

        [Fact]
        public void KeepAliveThrowsWhenNegative()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(-1));
        }

        [Fact]
        public void KeepAliveThrowsWhenZero()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(0));
        }

        [Fact]
        public void KeepAliveThrowsWhenLessThanTwoSeconds()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(1.99));
            config.KeepAlive = TimeSpan.FromSeconds(2);
        }

        [Fact]
        public void KeepAliveThrowsWhenGreaterThanAThirdOfTheDisconnectTimeout()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(10.01));
            config.KeepAlive = TimeSpan.FromSeconds(10);

            // Arrange
            config = new DefaultConfigurationManager();
            config.DisconnectTimeout = TimeSpan.FromSeconds(15);

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(5.01));
            config.KeepAlive = TimeSpan.FromSeconds(5);
        }

        [Fact]
        public void TwoSecondsAndNullOnlyValidKeepAliveValuesWhenDisconnectTimeoutIsSixSeconds()
        {
            // Arrange
            var config = new DefaultConfigurationManager();
            config.DisconnectTimeout = TimeSpan.FromSeconds(6);

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(1.99));
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(2.01));

            // Assert doesn't throw
            config.KeepAlive = TimeSpan.FromSeconds(2);
            config.KeepAlive = null;
        }

        [Fact]
        public void DisconnectTimeoutThrowsWhenNegative()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.KeepAlive = TimeSpan.FromSeconds(-1));
        }

        [Fact]
        public void DisconnectTimeoutThrowsWhenZero()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.DisconnectTimeout = TimeSpan.FromSeconds(0));
        }

        [Fact]
        public void DisconnectTimeoutThrowsWhenLessThanSixSeconds()
        {
            // Arrange
            var config = new DefaultConfigurationManager();

            // Assert
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.DisconnectTimeout = TimeSpan.FromSeconds(5.99));
            config.DisconnectTimeout = TimeSpan.FromSeconds(6);
        }

        [Fact]
        public void SettingDisconnectTimeoutSetKeepAliveToAThirdOfItself()
        {
            // Arrange
            var config = new DefaultConfigurationManager();
            var random = new Random();
            config.DisconnectTimeout = TimeSpan.FromSeconds(random.Next(6, 31536000)); // 6 seconds to a year

            // Assert
            Assert.Equal(TimeSpan.FromTicks(config.DisconnectTimeout.Ticks / 3), config.KeepAlive);
        }

        [Fact]
        public void KeepAliveCannotBeConfiguredBeforeDisconnectTimeout()
        {
            // Arrange
            var config = new DefaultConfigurationManager();
            config.KeepAlive = TimeSpan.FromSeconds(5);

            // Assert
            Assert.Throws(typeof(InvalidOperationException), () => config.DisconnectTimeout = TimeSpan.FromSeconds(40));

            // Arrange
            config = new DefaultConfigurationManager();
            config.KeepAlive = null;

            // Assert
            Assert.Throws(typeof(InvalidOperationException), () => config.DisconnectTimeout = TimeSpan.FromSeconds(40));
        }

        [Fact]
        public void KeepAliveTimeoutIsTwiceTheKeepAlive()
        {
            // Arrange
            var config = new DefaultConfigurationManager();
            var random = new Random();
            config.KeepAlive = TimeSpan.FromSeconds(random.NextDouble() * 8 + 2); // 2 to 10 seconds

            // Assert
            Assert.Equal(TimeSpan.FromTicks(config.KeepAlive.Value.Ticks * 2), config.KeepAliveTimeout());

            // Arrange
            config.KeepAlive = null;

            // Assert
            Assert.Equal(null, config.KeepAliveTimeout());
        }

        [Fact]
        public void HeartbeatIntervalIsHalfTheKeepAlive()
        {
            // Arrange
            var config = new DefaultConfigurationManager();
            var random = new Random();
            config.KeepAlive = TimeSpan.FromSeconds(random.NextDouble() * 8 + 2); // 2 to 10 seconds

            // Assert
            Assert.Equal(TimeSpan.FromTicks(config.KeepAlive.Value.Ticks / 2), config.HeartbeatInterval());
        }

        [Fact]
        public void HeartbeatIntervalIsASixthOfTheDisconnectTimeoutIfTheKeepAliveIsNull()
        {
            // Arrange
            var config = new DefaultConfigurationManager();
            var random = new Random();
            config.DisconnectTimeout = TimeSpan.FromSeconds(random.Next(6, 31536000)); // 6 seconds to a year
            config.KeepAlive = null;

            // Assert
            Assert.Equal(TimeSpan.FromTicks(config.DisconnectTimeout.Ticks / 6), config.HeartbeatInterval());
        }

        [Fact]
        public void TopicTimeToLiveIsDoubleTheDisconnectAndKeepAliveTimeouts()
        {
            var config = new DefaultConfigurationManager();
            var random = new Random();
            config.DisconnectTimeout = TimeSpan.FromSeconds(random.Next(12, 31536000)); // 12 seconds to a year
            config.KeepAlive = TimeSpan.FromTicks(config.DisconnectTimeout.Ticks / 6); // Set custom keep-alive to half the default

            // Assert
            Assert.Equal(TimeSpan.FromTicks(config.DisconnectTimeout.Ticks * 2 + config.KeepAliveTimeout().Value.Ticks * 2),
                         config.TopicTtl());
        }

        [Fact]
        public void TopicTopicTimeToLiveIsDoubleTheDisconnectTimeoutWhenKeepAliveIsNull()
        {
            var config = new DefaultConfigurationManager();
            var random = new Random();
            config.DisconnectTimeout = TimeSpan.FromSeconds(random.Next(6, 31536000)); // 12 seconds to a year
            config.KeepAlive = null;

            // Assert
            Assert.Equal(TimeSpan.FromTicks(config.DisconnectTimeout.Ticks * 2), config.TopicTtl());
        }
    }
}
