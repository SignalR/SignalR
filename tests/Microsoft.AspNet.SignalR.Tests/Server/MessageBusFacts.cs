using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class MessageBusFacts
    {
        [Fact]
        public void NewSubscriptionGetsAllMessages()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var wh = new ManualResetEventSlim(initialState: false);
                IDisposable subscription = null;

                try
                {
                    bus.Publish("test", "key", "1").Wait();

                    subscription = bus.Subscribe(subscriber, null, result =>
                    {
                        if (!result.Terminal)
                        {
                            var m = result.GetMessages().Single();

                            Assert.Equal("key", m.Key);
                            Assert.Equal("value", m.Value);

                            wh.Set();

                            return TaskAsyncHelper.True;
                        }

                        return TaskAsyncHelper.False;

                    }, 10);

                    bus.Publish("test", "key", "value").Wait();

                    Assert.True(wh.Wait(TimeSpan.FromSeconds(5)));
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
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var cd = new CountDownRange<int>(Enumerable.Range(2, 4));
                IDisposable subscription = null;

                bus.Publish("test", "key", "1").Wait();
                bus.Publish("test", "key", "2").Wait();
                bus.Publish("test", "key", "3").Wait();
                bus.Publish("test", "key", "4").Wait();

                try
                {
                    subscription = bus.Subscribe(subscriber, "key,00000001", result =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.Value);
                            Assert.True(cd.Mark(n));
                        }

                        return TaskAsyncHelper.True;

                    }, 10);

                    bus.Publish("test", "key", "5");

                    Assert.True(cd.Wait(TimeSpan.FromSeconds(5)));
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
        public void SubscriptionWithMultipleExistingCursors()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key", "key2" });
                var cdKey = new CountDownRange<int>(Enumerable.Range(2, 4));
                var cdKey2 = new CountDownRange<int>(new[] { 1, 2, 10 });
                IDisposable subscription = null;

                bus.Publish("test", "key", "1").Wait();
                bus.Publish("test", "key", "2").Wait();
                bus.Publish("test", "key", "3").Wait();
                bus.Publish("test", "key", "4").Wait();
                bus.Publish("test", "key2", "1").Wait();
                bus.Publish("test", "key2", "2").Wait();

                try
                {
                    subscription = bus.Subscribe(subscriber, "key,00000001|key2,00000000", result =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.Value);
                            if (m.Key == "key")
                            {
                                Assert.True(cdKey.Mark(n));
                            }
                            else
                            {
                                Assert.True(cdKey2.Mark(n));
                            }
                        }

                        return TaskAsyncHelper.True;

                    }, 10);

                    bus.Publish("test", "key", "5");
                    bus.Publish("test", "key2", "10");

                    Assert.True(cdKey.Wait(TimeSpan.FromSeconds(5)));
                    Assert.True(cdKey2.Wait(TimeSpan.FromSeconds(5)));
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
        public void AddingEventAndSendingMessages()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "a" });
                int max = 100;
                var cd = new CountDownRange<int>(Enumerable.Range(0, max));
                int prev = -1;
                IDisposable subscription = null;

                try
                {
                    subscription = bus.Subscribe(subscriber, null, result =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.Value);
                            Assert.True(prev < n, "out of order");
                            prev = n;
                            Assert.True(cd.Mark(n));
                        }

                        return TaskAsyncHelper.True;
                    }, 10);

                    for (int i = 0; i < max; i++)
                    {
                        subscriber.AddEvent("b");
                        bus.Publish("test", "b", i.ToString()).Wait();
                    }

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
        public void DisposingBusShutsWorkersDown()
        {
            var dr = new DefaultDependencyResolver();
            var bus = new MessageBus(dr);
            var subscriber = new TestSubscriber(new[] { "key" });
            var wh = new ManualResetEventSlim(initialState: false);
            IDisposable subscription = null;

            try
            {
                subscription = bus.Subscribe(subscriber, null, result =>
                {
                    if (!result.Terminal)
                    {
                        var m = result.GetMessages().Single();

                        Assert.Equal("key", m.Key);
                        Assert.Equal("value", m.Value);

                        wh.Set();

                        return TaskAsyncHelper.True;
                    }

                    return TaskAsyncHelper.False;

                }, 10);

                bus.Publish("test", "key", "value").Wait();

                Assert.True(wh.Wait(TimeSpan.FromSeconds(5))); 
            }
            finally
            {
                if (subscription != null)
                {
                    subscription.Dispose();
                }

                bus.Dispose();

                Assert.Equal(bus.AllocatedWorkers, 1);
                Assert.Equal(bus.BusyWorkers, 0);

                Thread.Sleep(TimeSpan.FromSeconds(5));

                Assert.Equal(bus.AllocatedWorkers, 0);
                Assert.Equal(bus.BusyWorkers, 0);
            }
        }
    }
}
