using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

                    subscription = bus.Subscribe(subscriber, null, (result, state) =>
                    {
                        if (!result.Terminal)
                        {
                            var m = result.GetMessages().Single();

                            Assert.Equal("key", m.Key);
                            Assert.Equal("value", m.GetString());

                            wh.Set();

                            return TaskAsyncHelper.True;
                        }

                        return TaskAsyncHelper.False;

                    }, 10, null);

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

        [Fact(Timeout = 5000)]
        public void SubscriptionWithCancelledTaskCanBeDisposed()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var wh = new ManualResetEventSlim();

                IDisposable subscription = bus.Subscribe(subscriber, null, async (result, state) =>
                {
                    if (result.Terminal)
                    {
                        return false;
                    }

                    await Task.Delay(50);
                    var tcs = new TaskCompletionSource<bool>();
                    tcs.SetCanceled();
                    wh.Set();
                    return await tcs.Task;

                }, 10, null);

                bus.Publish("me", "key", "hello");

                wh.Wait();

                subscription.Dispose();
            }
        }

        [Fact]
        public void PublishingDoesNotCreateTopic()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.KeepAlive = null;

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
            configuration.KeepAlive = null;

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
                    subscription = bus.Subscribe(subscriber, null, (result, state) => TaskAsyncHelper.True, 10, null);

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
        public void SubscribingTopicAfterNoSubscriptionsStateSetsStateToHasSubscription()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);

            using (var bus = new MessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });

                // Make sure the topic is in the no subs state
                bus.Subscribe(subscriber, null, (result, state) => TaskAsyncHelper.True, 10, null)
                   .Dispose();

                Topic topic = bus.SubscribeTopic("key");
                Assert.Equal(1, bus.Topics.Count);
                Assert.True(bus.Topics.TryGetValue("key", out topic));
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
            }
        }

        [Fact]
        public void SubscribingTopicAfterNoSubscriptionsWhenGCStateSetsStateToHasSubscription()
        {
            var dr = new DefaultDependencyResolver();

            using (var bus = new TestMessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                int retries = 0;
                // Make sure the topic is in the no subs state
                bus.Subscribe(subscriber, null, (result, state) => TaskAsyncHelper.True, 10, null)
                   .Dispose();

                bus.BeforeTopicMarked = (key, t) =>
                {
                    if (retries == 0)
                    {
                        // Need to garbage collect twice to force the topic into the dead state
                        bus.GarbageCollectTopics();
                        Assert.Equal(TopicState.Dying, t.State);
                    }
                    retries++;
                };

                bus.AfterTopicMarked = (key, t, state) =>
                {
                    if (retries == 1)
                    {
                        // Assert that we've revived the topic from dying since we've subscribed to the topic
                        Assert.Equal(TopicState.HasSubscriptions, state);
                    }
                };

                Topic topic = bus.SubscribeTopic("key");
                Assert.Equal(1, bus.Topics.Count);
                Assert.True(bus.Topics.TryGetValue("key", out topic));
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
                Assert.Equal(1, retries);
            }
        }

        [Fact]
        public void MultipleSubscribeTopicCallsToDeadTopicWork()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            Topic topic;
            configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
            configuration.KeepAlive = null;

            using (var bus = new TestMessageBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                int count = 0;

                // Make sure the topic is in the no subs state
                bus.Subscribe(subscriber, null, (result, state) => TaskAsyncHelper.True, 10, null)
                   .Dispose();

                bus.BeforeTopicCreated = (key) =>
                {
                    bus.Topics.TryGetValue(key, out topic);

                    if (count == 1)
                    {
                        // Should have been removed by our double garbage collect in BeforeTopicMarked
                        Assert.Null(topic);
                    }

                    if (count == 3)
                    {
                        // Ensure that we have a topic now created from the original thread
                        Assert.NotNull(topic);
                    }
                };

                bus.BeforeTopicMarked = (key, t) =>
                {
                    count++;

                    if (count == 1)
                    {
                        bus.GarbageCollectTopics();
                        bus.GarbageCollectTopics();
                        // We garbage collect twice to mark the current topic as dead (it will remove it from the topics list)

                        Assert.Equal(t.State, TopicState.Dead);

                        bus.SubscribeTopic("key");

                        // Topic should still be dead
                        Assert.Equal(t.State, TopicState.Dead);
                        Assert.Equal(count, 2);

                        // Increment up to 3 so we don't execute same code path in after marked
                        count++;
                    }

                    if (count == 2)
                    {
                        // We've just re-created the topic from the second bus.SubscribeTopic so we should have 0 subscriptions
                        Assert.Equal(t.State, TopicState.NoSubscriptions);
                    }

                    if (count == 4)
                    {
                        // Ensure that we pulled the already created subscription (therefore it has subscriptions)
                        Assert.Equal(t.State, TopicState.HasSubscriptions);
                    }
                };

                bus.AfterTopicMarked = (key, t, state) =>
                {
                    if (count == 2)
                    {
                        // After re-creating the topic from the second bus.SubscribeTopic we should then move the topic state
                        // into the has subscriptions state
                        Assert.Equal(state, TopicState.HasSubscriptions);
                    }

                    if (count == 3)
                    {
                        Assert.Equal(state, TopicState.Dead);
                    }
                };

                bus.SubscribeTopic("key");
                Assert.Equal(1, bus.Topics.Count);
                Assert.True(bus.Topics.TryGetValue("key", out topic));
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
            }
        }

        [Fact]
        public void GetTopicDoesNotChangeStateWhenNotDying()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
            configuration.KeepAlive = null;

            using (var bus = new MessageBus(dr))
            {
                bus.Subscribe(new TestSubscriber(new[] { "key" }), null, (result, state) => TaskAsyncHelper.True, 10, null);
                Topic topic;
                Assert.True(bus.Topics.TryGetValue("key", out topic));
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
                topic = bus.GetTopic("key");
                Assert.Equal(TopicState.HasSubscriptions, topic.State);
                topic.RemoveSubscription(topic.Subscriptions.First());
                Assert.Equal(TopicState.NoSubscriptions, topic.State);
                topic = bus.GetTopic("key");
                Assert.Equal(TopicState.NoSubscriptions, topic.State);
                topic.State = TopicState.Dying;
                topic = bus.GetTopic("key");
                Assert.Equal(TopicState.NoSubscriptions, topic.State);
            }
        }

        [Fact]
        public void GarbageCollectingTopicsBeforeSubscribingTopicSetsStateToHasSubscription()
        {
            var dr = new DefaultDependencyResolver();
            var configuration = dr.Resolve<IConfigurationManager>();
            configuration.DisconnectTimeout = TimeSpan.FromSeconds(6);
            configuration.KeepAlive = null;

            using (var bus = new MessageBus(dr))
            {
                bus.BeforeTopicMarked = (key, t) =>
                {
                    bus.GarbageCollectTopics();
                };

                Topic topic = bus.SubscribeTopic("key");
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
                Func<TestSubscriber> subscriberFactory = () => new TestSubscriber(new[] { "key" });
                var cd = new CountDownRange<int>(Enumerable.Range(2, 4));
                IDisposable subscription = null;
                string prefix = DefaultSubscription._defaultCursorPrefix;

                // Pretend like we had an initial subscription
                bus.Subscribe(subscriberFactory(), null, (result, state) => TaskAsyncHelper.True, 10, null)
                   .Dispose();

                bus.Publish("test", "key", "1").Wait();
                bus.Publish("test", "key", "2").Wait();
                bus.Publish("test", "key", "3").Wait();
                bus.Publish("test", "key", "4").Wait();

                try
                {
                    subscription = bus.Subscribe(subscriberFactory(), prefix + "key,00000001", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());
                            Assert.True(cd.Mark(n));
                        }

                        return TaskAsyncHelper.True;

                    }, 10, null);

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
                Func<ISubscriber> subscriberFactory = () => new TestSubscriber(new[] { "key", "key2" });
                var cdKey = new CountDownRange<int>(Enumerable.Range(2, 4));
                var cdKey2 = new CountDownRange<int>(new[] { 1, 2, 10 });
                IDisposable subscription = null;

                string prefix = DefaultSubscription._defaultCursorPrefix;

                // Pretend like we had an initial subscription
                bus.Subscribe(subscriberFactory(), null, (result, state) => TaskAsyncHelper.True, 10, null)
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
                    subscription = bus.Subscribe(subscriberFactory(), prefix + "key,00000001|key2,00000000", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());
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

                    }, 10, null);

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
                    subscription = bus.Subscribe(subscriber, "d-key,00000001", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            Assert.Equal("key", m.Key);
                            Assert.Equal("value", m.GetString());
                            wh.Set();
                        }

                        return TaskAsyncHelper.True;

                    }, 10, null);

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
        public void SubscriptionWithScaleoutCursorGetsOnlyNewMessages()
        {
            var dr = new DefaultDependencyResolver();
            var passThroughMinfier = new PassThroughStringMinifier();
            dr.Register(typeof(IStringMinifier), () => passThroughMinfier);
            using (var bus = new MessageBus(dr))
            {
                Func<ISubscriber> subscriberFactory = () => new TestSubscriber(new[] { "key" });
                var tcs = new TaskCompletionSource<Message[]>();
                IDisposable subscription = null;

                try
                {
                    // Set-up dummy subscription so the first Publish doesn't noop
                    bus.Subscribe(subscriberFactory(), null, (result, state) => TaskAsyncHelper.True, 10, null).Dispose();

                    bus.Publish("test", "key", "badvalue").Wait();

                    subscription = bus.Subscribe(subscriberFactory(), "s-key,00000000", (result, state) =>
                    {
                        tcs.TrySetResult(result.GetMessages().ToArray());
                        return TaskAsyncHelper.True;
                    }, 10, null);

                    bus.Publish("test", "key", "value");

                    Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(5)));

                    foreach (var m in tcs.Task.Result)
                    {
                        Assert.Equal("key", m.Key);
                        Assert.Equal("value", m.GetString());
                    }
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
                    subscription = bus.Subscribe(subscriber, null, (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());
                            Assert.True(prev < n, "out of order");
                            prev = n;
                            Assert.True(cd.Mark(n));
                        }

                        return TaskAsyncHelper.True;
                    }, 10, null);

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
        public void MessageBusCanBeDisposedTwiceWithoutHanging()
        {
            var bus = new MessageBus(new DefaultDependencyResolver());

            bus.Dispose();
            Assert.True(Task.Run(() => bus.Dispose()).Wait(TimeSpan.FromSeconds(10)));
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
