using System;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ServiceBusConfigurationFacts
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("connection", null)]
        [InlineData("connection", "")]
        [InlineData(null, "topic")]
        [InlineData("", "topic")]
        public void ValidateArguments(string connectionString, string topicPrefix)
        {
            Assert.Throws<ArgumentNullException>(() => new ServiceBusScaleoutConfiguration(connectionString, topicPrefix));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidateTopicCount(int topicCount)
        {
            var config = new ServiceBusScaleoutConfiguration("cs", "topic");
            Assert.Throws<ArgumentOutOfRangeException>(() => config.TopicCount = topicCount);
        }

        [Fact]
        public void PositiveTopicCountsWork()
        {
            var config = new ServiceBusScaleoutConfiguration("cs", "topic");
            config.TopicCount = 1;
        }
    }
}
