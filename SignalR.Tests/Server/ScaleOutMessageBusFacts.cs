using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Tests.Infrastructure;
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

        [Fact]
        public void SubscriptionWithExistingCursor()
        {
            var dr = new DefaultDependencyResolver();
            var bus = new TestScaleoutBus(dr, topicCount: 2);
            var subscriber = new TestSubscriber(new[] { "key" });
            var cd = new CountDownRange<int>(Enumerable.Range(2, 4));
            IDisposable subscription = null;

            // test, test2 = 1
            // test1, test3 = 0
            // 

            // Cursor 1, 1
            bus.SendMany(new[] { 
                 new Message("test", "key", "1"),
                 new Message("test", "key", "50")
            });

            // Cursor 0,1|1,1
            bus.SendMany(new[] {
                new Message("test1", "key", "51")
            });

            bus.SendMany(new[]{
                 new Message("test2", "key", "2"),
                 new Message("test3", "key", "3"),
                 new Message("test2", "key", "4"),
            });

            try
            {
                subscription = bus.Subscribe(subscriber, "0,00000001|1,00000001", result =>
                {
                    foreach (var m in result.GetMessages())
                    {
                        int n = Int32.Parse(m.Value);
                        Assert.True(cd.Mark(n));
                    }

                    return TaskAsyncHelper.True;

                }, 10);

                bus.SendMany(new[] { new Message("test", "key", "5") });

                Assert.True(cd.Wait(TimeSpan.FromSeconds(10)));
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
                    OnReceived(topic.ToString(), (ulong)Interlocked.Increment(ref _topics[topic]), g.ToArray()).Wait();
                }
                return TaskAsyncHelper.Empty;
            }
        }
    }
}
