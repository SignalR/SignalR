using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Configuration;
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
                    subscription = bus.Subscribe(subscriber, "s-0,00000000|1,00000000", (result, state) =>
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
        public void SubscriptionPullFromMultipleStreamsInFairOrder()
        {
            var dr = new DefaultDependencyResolver();
            using (var bus = new TestScaleoutBus(dr, streams: 3))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                var cd = new OrderedCountDownRange<int>(new[] { 1, 2, 4, 3 });
                IDisposable subscription = null;

                bus.Publish(0, 1, new[] { 
                        new Message("test", "key", "3"),
                        new Message("test", "key2", "5"),
                    },
                    new DateTime(TimeSpan.TicksPerDay * 5, DateTimeKind.Local));

                bus.Publish(1, 1, new[] {
                        new Message("test", "key", "1"),
                        new Message("test", "key2", "foo")
                    },
                    new DateTime(TimeSpan.TicksPerDay * 1, DateTimeKind.Local));

                bus.Publish(2, 1, new[] {
                        new Message("test", "key", "2"),
                        new Message("test", "key", "4")
                    },
                    new DateTime(TimeSpan.TicksPerDay * 2, DateTimeKind.Local));

                try
                {
                    subscription = bus.Subscribe(subscriber, "s-0,0|1,0|2,0", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());
                            Assert.True(cd.Expect(n));
                        }

                        return TaskAsyncHelper.True;

                    }, 10, null);

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
        public void SubscriptionGetsNewMessagesWhenTopicStoreOverrun()
        {
            var dr = new DefaultDependencyResolver();
            dr.Resolve<IConfigurationManager>().DefaultMessageBufferSize = 10;

            using (var bus = new TestScaleoutBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;
                // 16-49 is the valid range
                var cd = new OrderedCountDownRange<int>(Enumerable.Range(16, 33));
                var results = new List<bool>();

                for (int i = 0; i < 50; i++)
                {
                    bus.Publish(0, (ulong)i, new[] { 
                        new Message("test", "key", i.ToString())
                    });
                }

                try
                {
                    subscription = bus.Subscribe(subscriber, "s-0,1", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());

                            cd.Expect(n);
                        }

                        return TaskAsyncHelper.True;

                    }, 10, null);

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
        public void SubscriptionDoesNotGetNewMessagesWhenTopicStoreOverrunByOtherStream()
        {
            var dr = new DefaultDependencyResolver();
            dr.Resolve<IConfigurationManager>().DefaultMessageBufferSize = 10;

            using (var bus = new TestScaleoutBus(dr, streams: 2))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;

                // The min fragment size is 8 and min fragments is 5
                var expectedValues = Enumerable.Range(171, 8);
                var cd = new OrderedCountDownRange<int>(expectedValues);

                // This will overwrite the buffer ending up with (40 - 79) for stream 2
                for (int i = 0; i < 80; i++)
                {
                    bus.Publish(0, (ulong)i, new[] { 
                        new Message("test", "key", i.ToString())
                    });
                }

                // This will overwrite the buffer with (140 - 179) for stream 1
                for (int i = 100; i < 180; i++)
                {
                    bus.Publish(1, (ulong)i, new[] { 
                        new Message("test", "key", i.ToString())
                    });
                }

                try
                {
                    subscription = bus.Subscribe(subscriber, "s-0,27|1,AA", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());

                            cd.Expect(n);
                        }

                        return TaskAsyncHelper.True;

                    }, 100, null);

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
        public void SubscriptionGetsCorrectCursorsIfMoreKeysThanStreams()
        {
            var dr = new DefaultDependencyResolver();

            using (var bus = new TestScaleoutBus(dr))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;
                var cd = new OrderedCountDownRange<int>(new[] { 101 });

                bus.Publish(0, 10ul, new[] { 
                    new Message("test", "key", "100")
                });

                try
                {
                    subscription = bus.Subscribe(subscriber, "s-0,0|1,0|2,4", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());
                            cd.Expect(n);
                        }

                        return TaskAsyncHelper.True;

                    }, 100, null);

                    bus.Publish(0, 11ul, new[] { 
                        new Message("test", "key", "101")
                    });

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
        public void SubscriptionGetsCorrectCursorsIfLessKeysThanStreams()
        {
            var dr = new DefaultDependencyResolver();

            using (var bus = new TestScaleoutBus(dr, streams: 2))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;
                var cd = new OrderedCountDownRange<int>(new[] { 101, 11 });

                bus.Publish(0, 10ul, new[] { 
                    new Message("test", "key", "100")
                });

                bus.Publish(1, 10ul, new[] { 
                    new Message("test", "key", "10")
                });

                try
                {
                    subscription = bus.Subscribe(subscriber, "s-0,0", (result, state) =>
                    {
                        foreach (var m in result.GetMessages())
                        {
                            int n = Int32.Parse(m.GetString());
                            cd.Expect(n);
                        }

                        return TaskAsyncHelper.True;

                    }, 100, null);

                    bus.Publish(0, 11ul, new[] { 
                        new Message("test", "key", "101")
                    }, 
                    new DateTime(TimeSpan.TicksPerDay * 1));

                    bus.Publish(1, 11ul, new[] { 
                        new Message("test", "key", "11")
                    },
                    new DateTime(TimeSpan.TicksPerDay * 2));

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
        public void SubscriptionWithDefaultCursorGetsOnlyNewMessages()
        {
            var dr = new DefaultDependencyResolver();

            using (var bus = new TestScaleoutBus(dr, streams: 1))
            {
                var subscriber = new TestSubscriber(new[] { "key" });
                IDisposable subscription = null;
                var tcs = new TaskCompletionSource<Message[]>();

                bus.Publish(0, 1ul, new[] { 
                    new Message("test", "key", "badvalue")
                });

                try
                {
                    subscription = bus.Subscribe(subscriber, "d-0,0", (result, state) =>
                    {
                        tcs.TrySetResult(result.GetMessages().ToArray());
                        return TaskAsyncHelper.True;
                    }, 100, null);

                    bus.Publish(0, 2ul, new[] {
                        new Message("test", "key", "value")
                    });

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
                Publish(streamIndex, id, messages, DateTime.UtcNow);
            }

            public void Publish(int streamIndex, ulong id, IList<Message> messages, DateTime creationTime)
            {
                var message = new ScaleoutMessage
                {
                    Messages = messages,
                    ServerCreationTime = creationTime,
                };

                OnReceived(streamIndex, id, message);
            }
        }
    }
}
