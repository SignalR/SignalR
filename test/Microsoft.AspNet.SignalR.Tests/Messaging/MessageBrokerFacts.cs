using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Messaging
{
    public class MessageBrokerFacts
    {
        [Fact]
        public async Task DoWorkDisposesSubscriptionIfWorkThrowsInline()
        {
            var subscription = new TestSubscription("test", () => throw new InvalidOperationException());
            var counterManager = new TestPerformanceCounterManager();
            var broker = new MessageBroker(counterManager) { Trace = new TraceSource($"SignalR.{nameof(MessageBroker)}.Test") };
            var context = new MessageBroker.WorkContext(subscription, broker);

            await MessageBroker.DoWork(context);

            Assert.True(subscription.Disposed);
        }

        [Fact]
        public async Task DoWorkDisposesSubscriptionIfWorkReturnsFaultedTask()
        {
            var subscription = new TestSubscription("test", () => Task.FromException(new InvalidOperationException()));
            var counterManager = new TestPerformanceCounterManager();
            var broker = new MessageBroker(counterManager) { Trace = new TraceSource($"SignalR.{nameof(MessageBroker)}.Test") };
            var context = new MessageBroker.WorkContext(subscription, broker);

            await MessageBroker.DoWork(context);

            Assert.True(subscription.Disposed);
        }
    }
}
