using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SignalR.Tests.Server
{
    public class ScaleOutMessageBusFacts
    {
        [Fact]
        public void NewSubscriptionGetsAllMessages()
        {
            var dr = new DefaultDependencyResolver();
            var bus = new TestScaleoutBus(dr, topicCount: 5);
            var subscriber = new TestSubscriber(new[] { "key" });
            var wh = new ManualResetEventSlim(initialState: false);
            IDisposable subscription = null;

            try
            {
                var firstMessages = new[] { new Message("test1", "key", "1"),
                                            new Message("test2", "key", "2") };

                bus.SendMany(firstMessages);

                subscription = bus.Subscribe(subscriber, null, result =>
                {
                    if (!result.Terminal)
                    {
                        var ms = result.GetMessages().ToList();

                        Assert.Equal(2, ms.Count);
                        Assert.Equal("key", ms[0].Key);
                        Assert.Equal("x", ms[0].Value);
                        Assert.Equal("key", ms[1].Key);
                        Assert.Equal("y", ms[1].Value);

                        wh.Set();

                        return TaskAsyncHelper.True;
                    }

                    return TaskAsyncHelper.False;

                }, 10);

                bus.SendMany(new[] { new Message("test1", "key", "x"), 
                                     new Message("test1", "key", "y") });

                Assert.True(wh.Wait(TimeSpan.FromSeconds(10)));
            }
            finally
            {
                if (subscription != null)
                {
                    subscription.Dispose();
                }
            }
        }

        private class TestScaleoutBus : ScaleoutMessageBus
        {
            private long[] _topics;

            public TestScaleoutBus(IDependencyResolver resolver, int topicCount = 1)
                : base(resolver)
            {
                _topics = new long[topicCount];
            }

            protected override void Initialize()
            {

            }

            public Task SendMany(Message[] messages)
            {
                return Send(messages);
            }

            protected override Task Send(Message[] messages)
            {
                foreach (var g in messages.GroupBy(m => m.Source))
                {
                    int topic = Math.Abs(g.Key.GetHashCode()) % _topics.Length;
                    OnReceived(g.Key, (ulong)Interlocked.Increment(ref _topics[topic]), g.ToArray()).Wait();
                }
                return TaskAsyncHelper.Empty;
            }
        }
    }
}
