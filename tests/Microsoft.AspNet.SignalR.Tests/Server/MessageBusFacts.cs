using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Infrastructure;
using Moq;
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
        public void PublishingDoesNotCreateTopic()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.DisconnectTimeout = TimeSpan.Zero;
            configuration.KeepAlive = 0;

            using (var bus = new MessageBus(dr))
            {
                bus.Publish("test", "key", "1").Wait();

                Assert.Equal(0, bus.Topics.Count);
                Assert.False(bus.Topics.ContainsKey("key"));
            }
        }

        [Fact]
        public void GarbageCollectingTopicsAfterGettingTopicsNoops()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.DisconnectTimeout = TimeSpan.Zero;
            configuration.KeepAlive = 0;

            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;
                bus.AfterTopicMarkedSuccessfully = (key, topic) =>
                {
                    bus.GarbageCollectTopics();
                };

                try
                {
                    subscription = bus.Subscribe(subscriber, null, result => TaskAsyncHelper.True, 10);

                    Assert.Equal(1, bus.Topics.Count);
                    Topic topic;
                    Assert.True(bus.Topics.TryGetValue("key", out topic));
                    Assert.Equal(TopicState.HasSubscriptions, topic.State);
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
        public void GettingTopicAfterNoSubscriptionsStateSetsStateToHasSubscriptions()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.DisconnectTimeout = TimeSpan.Zero;

            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });

                // Make sure the topic is in the no subs state
                bus.Subscribe(subscriber, null, _ => TaskAsyncHelper.True, 10)
                   .Dispose();

                Topic topic = bus.GetTopic("key");
                Assert.Equal(1, bus.Topics.Count);
                Assert.True(bus.Topics.TryGetValue("key", out topic));
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
            }
        }

        [Fact]
        public void GettingTopicAfterNoSubscriptionsWhenGCStateSetsStateToHasSubscriptions()
        {
            var dr = new DefaultDependencyResolver();
            
            using (var bus = new TestMessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                int retries = 0;
                // Make sure the topic is in the no subs state
                bus.Subscribe(subscriber, null, _ => TaskAsyncHelper.True, 10)
                   .Dispose();

                bus.BeforeTopicMarked = (key, t) =>
                {
                    if (retries == 0)
                    {
                        bus.GarbageCollectTopics();
                    }
                    retries++;
                };

                bus.AfterTopicMarked = (key, t, state) =>
                {
                    if (retries == 1)
                    {
                        Assert.Equal(TopicState.Dead, state);
                    }
                };

                Topic topic = bus.GetTopic("key");
                Assert.Equal(1, bus.Topics.Count);
                Assert.True(bus.Topics.TryGetValue("key", out topic));
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
                Assert.Equal(2, retries);
            }
        }

        [Fact]
        public void GarbageCollectingTopicsBeforeGettingTopicSetsStateToHasSubscriptions()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.DisconnectTimeout = TimeSpan.Zero;
            configuration.KeepAlive = 0;

            using (var bus = new MessageBus(dr))
            {
                bus.BeforeTopicMarked = (key, t) =>
                {
                    bus.GarbageCollectTopics();
                };

                Topic topic = bus.GetTopic("key");
                Assert.Equal(1, bus.Topics.Count);
                Assert.True(bus.Topics.TryGetValue("key", out topic));
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
            }
        }

        [Fact]
        public void SubscriptionWithExistingCursor()
        {
            var dr = new DefaultDependencyResolver();
            var passThroughMinfier = new PassThroughStringMinifier();
            dr.Register(typeof(IStringMinifier), () => passThroughMinfier);
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var cd = new CountDownRange<int>(Enumerable.Range(2, 4));
                IDisposable subscription = null;

                // Pretend like we had an initial subscription
                bus.Subscribe(subscriber, null, _ => TaskAsyncHelper.True, 10)
                   .Dispose();

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
            var passThroughMinfier = new PassThroughStringMinifier();
            dr.Register(typeof(IStringMinifier), () => passThroughMinfier);
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key", "key2" });
                var cdKey = new CountDownRange<int>(Enumerable.Range(2, 4));
                var cdKey2 = new CountDownRange<int>(new[] { 1, 2, 10 });
                IDisposable subscription = null;

                // Pretend like we had an initial subscription
                bus.Subscribe(subscriber, null, result => TaskAsyncHelper.True, 10)
                    .Dispose();

                // This simulates a reconnect
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
        public void SubscriptionWithExistingCursorGetsAllMessagesAfterMessageBusRestart()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var wh = new ManualResetEvent(false);
                IDisposable subscription = null;

                try
                {
                    subscription = bus.Subscribe(subscriber, "key,00000001", result =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            Assert.Equal("key", m.Key);
                            Assert.Equal("value", m.Value);
                            wh.Set();
                        }

                        return TaskAsyncHelper.True;

                    }, 10);

                    bus.Publish("test", "key", "value");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
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

                Assert.Equal(bus.AllocatedWorkers, 1);

                bus.Dispose();

                Assert.Equal(bus.AllocatedWorkers, 0);
            }
        }

        private class TestMessageBus : MessageBus
        {
            public TestMessageBus(IDependencyResolver resolver)
                : base(resolver)
            {

            }

            protected override Topic CreateTopic(string key)
            {
                var mock = new Mock<Topic>((uint)100, TimeSpan.Zero) { CallBase = true };
                mock.Setup(m => m.IsExpired).Returns(true);
                return mock.Object;
            }
        }

        private class PassThroughStringMinifier : IStringMinifier
        {
            public string Minify(string s)
            {
                return s;
            }

            public string Unminify(string s)
            {
                return s;
            }

            public void RemoveUnminified(string s)
            {
            }
        }
    }
}
