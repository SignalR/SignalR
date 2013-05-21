using System;
using Microsoft.AspNet.SignalR.Messaging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class TopicFacts
    {
        [Fact]
        public void TopicStateCreated()
        {
            var topic = new Topic(100, TimeSpan.Zero);
            Assert.Equal(TopicState.NoSubscriptions, topic.State);
        }

        [Fact]
        public void TopicStateHasSubscriptions()
        {
            var topic = new Topic(100, TimeSpan.Zero);

            topic.AddSubscription(new Mock<ISubscription>().Object);

            Assert.Equal(TopicState.HasSubscriptions, topic.State);
        }

        [Fact]
        public void TopicStateHasSubscriptionsIfMoreThanOne()
        {
            var topic = new Topic(100, TimeSpan.Zero);
            var sub1 = new Mock<ISubscription>();
            sub1.Setup(m => m.Identity).Returns("1");

            topic.AddSubscription(new Mock<ISubscription>().Object);
            topic.AddSubscription(sub1.Object);
            topic.RemoveSubscription(sub1.Object);

            Assert.Equal(TopicState.HasSubscriptions, topic.State);
        }

        [Fact]
        public void TopicStateNoSubscriptions()
        {
            var topic = new Topic(100, TimeSpan.Zero);
            var mock = new Mock<ISubscription>();
            mock.Setup(m => m.Identity).Returns("1");

            topic.AddSubscription(mock.Object);
            topic.RemoveSubscription(mock.Object);

            Assert.Equal(TopicState.NoSubscriptions, topic.State);
        }
    }
}
