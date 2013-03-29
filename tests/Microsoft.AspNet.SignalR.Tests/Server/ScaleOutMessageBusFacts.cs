using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ScaleOutMessageBusFacts
    {
        [Fact]
        public void NewSubscriptionGetsAllMessages()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new TestScaleoutBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var wh = new ManualResetEventSlim(initialState: false);
                IDisposable subscription = null;

                try
                {
                    var firstMessages = new[] { new Message("test1", "key", "1"),
                                                new Message("test2", "key", "2") };

                    bus.Publish(0, 0, firstMessages);

                    subscription = bus.Subscribe(subscriber, null, (result, state) =>
                    {
                        if (!result.Terminal)
                        {
                            var ms = result.GetMessages().ToList();

                            Assert.Equal(2, ms.Count);
                            Assert.Equal("key", ms[0].Key);
                            Assert.Equal("x", ms[0].GetString());
                            Assert.Equal("key", ms[1].Key);
                            Assert.Equal("y", ms[1].GetString());

                            wh.Set();

                            return TaskAsyncHelper.True;
                        }

                        return TaskAsyncHelper.False;

                    }, 10, null);

                    var messages = new[] { new Message("test1", "key", "x"), 
                                           new Message("test1", "key", "y") 
                                         };
                    bus.Publish(0, 1, messages);

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
        }

        [Fact]
        public void SubscriptionWithExistingCursor()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new TestScaleoutBus(dr, streams: 2))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var cd = new CountDownRange<int>(Enumerable.Range(2, 4));
                IDisposable subscription = null;

                bus.Publish(0, 0, new[] { 
                    new Message("test", "key", "1"),
                    new Message("test", "key", "50")
                });

                bus.Publish(1, 0, new[] {
                    new Message("test1", "key", "51")
                });

                bus.Publish(1, 2, new[]{
                 new Message("test2", "key", "2"),
                 new Message("test3", "key", "3"),
                 new Message("test2", "key", "4"),
            });

                try
                {
                    subscription = bus.Subscribe(subscriber, "0,00000000|1,00000000", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());
                            Assert.True(cd.Mark(n));
                        }

                        return TaskAsyncHelper.True;

                    }, 10, null);

                    bus.Publish(0, 2, new[] { new Message("test", "key", "5") });

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
        }

        [Fact]
        public void SubscriptionPublishingAfter()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new TestScaleoutBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;
                var wh = new ManualResetEventSlim();

                try
                {
                    subscription = bus.Subscribe(subscriber, null, (result, state) =>
                    {
                        if (!result.Terminal)
                        {
                            var messages = result.GetMessages().ToList();
                            Assert.Equal(1, messages.Count);
                            Assert.Equal("connected", messages[0].GetString());
                            wh.Set();

                        }

                        return TaskAsyncHelper.True;

                    }, 10, null);

                    bus.Publish(0, 0, new[] { new Message("test", "key", "connected") });

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
        }

        [Fact(Skip = "This isn't consistent yet")]
        public void PayloadIdReset()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new TestScaleoutBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;
                var cd = new CountDownRange<int>(Enumerable.Range(1, 4));
                var wh = new ManualResetEventSlim();

                try
                {
                    subscription = bus.Subscribe(subscriber, null, (result, state) =>
                    {
                        if (!result.Terminal)
                        {
                            foreach (var m in result.GetMessages())
                            {
                                int n = Int32.Parse(m.GetString());
                                Assert.True(cd.Mark(n));
                            }
                        }

                        return TaskAsyncHelper.True;

                    }, 10, null);

                    bus.Publish(0, 0, new[] { new Message("test", "key", "1") });
                    bus.Publish(0, 1, new[] { new Message("test", "key", "2") });
                    bus.Publish(0, 2, new[] { new Message("test", "key", "3") });
                    bus.Publish(0, 0, new[] { new Message("test", "key", "4") });

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
        }

        private class TestScaleoutBus : ScaleoutMessageBus
        {
            private int _streams;

            public TestScaleoutBus(IDependencyResolver resolver)
                : this(resolver, streams: 1)
            {
            }

            public TestScaleoutBus(IDependencyResolver dr, int streams)
                : base(dr, new ScaleoutConfiguration())
            {
                _streams = streams;
            }

            protected override int StreamCount
            {
                get
                {
                    return _streams;
                }
            }

            public void Publish(int streamIndex, ulong id, IList<Message> messages)
            {
                OnReceived(streamIndex, id, messages);
            }
        }
    }
}
