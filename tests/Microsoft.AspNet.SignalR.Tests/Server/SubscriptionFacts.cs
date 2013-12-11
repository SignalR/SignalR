using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Utilities;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class SubscriptionFacts
    {
        [Fact]
        public void CancelledTaskShouldThrowTaskCancelledSync()
        {
            Func<MessageResult, object, Task<bool>> callback = (result, state) =>
            {
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetCanceled();
                return tcs.Task;
            };

            var subscription = new Mock<TestSubscription>("TestSub", new[] { "a" }, callback, 1)
            {
                CallBase = true
            };

            using (subscription.Object)
            {
                Task task = null;
                TestUtilities.AssertUnwrappedException<TaskCanceledException>(() =>
                {
                    task = subscription.Object.Work();
                    task.Wait();
                });

                Assert.True(task.IsCanceled);
            }
        }

        [Fact(Timeout = 5000)]
        public void CancelledTaskShouldThrowTaskCancelledAsync()
        {
            Func<MessageResult, object, Task<bool>> callback = async (result, state) =>
            {
                await Task.Delay(500);
                var tcs = new TaskCompletionSource<bool>();
                tcs.SetCanceled();
                return await tcs.Task;
            };

            var subscription = new Mock<TestSubscription>("TestSub", new[] { "a" }, callback, 1)
            {
                CallBase = true
            };

            using (subscription.Object)
            {
                Task task = subscription.Object.Work();
                TestUtilities.AssertUnwrappedException<TaskCanceledException>(() => task.Wait());
                Assert.True(task.IsCanceled);
            }
        }

        [Fact]
        public void FaultedTaskShouldPropagateSync()
        {
            Func<MessageResult, object, Task<bool>> callback = async (result, state) =>
            {
                await TaskAsyncHelper.FromError(new Exception());
                return false;
            };

            var subscription = new Mock<TestSubscription>("TestSub", new[] { "a" }, callback, 1)
            {
                CallBase = true
            };

            using (subscription.Object)
            {
                Task task = null;
                TestUtilities.AssertUnwrappedException<Exception>(() =>
                {
                    task = subscription.Object.Work();
                    task.Wait();
                });

                Assert.True(task.IsFaulted);
            }
        }

        [Fact(Timeout = 5000)]
        public void FaultedTaskShouldPropagateAsync()
        {
            Func<MessageResult, object, Task<bool>> callback = async (result, state) =>
            {
                await Task.Delay(500);
                await TaskAsyncHelper.FromError(new Exception());
                return false;
            };

            var subscription = new Mock<TestSubscription>("TestSub", new[] { "a" }, callback, 1)
            {
                CallBase = true
            };

            using (subscription.Object)
            {
                Task task = subscription.Object.Work();
                TestUtilities.AssertUnwrappedException<Exception>(() => task.Wait());
                Assert.True(task.IsFaulted);
            }
        }

        [Fact(Timeout = 5000)]
        public void NoItemsCompletesTask()
        {
            Func<MessageResult, object, Task<bool>> callback = (result, state) =>
            {
                return TaskAsyncHelper.False;
            };

            var subscription = new Mock<TestSubscription>("TestSub", new[] { "a" }, callback, 0)
            {
                CallBase = true
            };

            using (subscription.Object)
            {
                Task task = subscription.Object.Work();
                Assert.True(task.IsCompleted);
            }
        }

        [Fact(Timeout = 5000)]
        public void ReturningFalseFromCallbackCompletesTaskAndDisposesSubscription()
        {
            Func<MessageResult, object, Task<bool>> callback = (result, state) =>
            {
                return TaskAsyncHelper.False;
            };

            var subscription = new Mock<TestSubscription>("TestSub", new[] { "a" }, callback, 1)
            {
                CallBase = true
            };

            var disposableMock = new Mock<IDisposable>();
            subscription.Object.Disposable = disposableMock.Object;
            disposableMock.Setup(m => m.Dispose()).Verifiable();

            using (subscription.Object)
            {
                Task task = subscription.Object.Work();
                Assert.True(task.IsCompleted);
                task.Wait();
                disposableMock.Verify(m => m.Dispose(), Times.Once());
            }
        }

        public class TestSubscription : Subscription
        {
            private readonly int _itemCount;
            public TestSubscription(string identity, IList<string> eventKeys, Func<MessageResult, object, Task<bool>> callback, int itemCount)
                : base(identity, eventKeys, callback, 10, GetCounters(), state: null)
            {
                _itemCount = itemCount;
            }

            protected override void PerformWork(IList<ArraySegment<Message>> items, out int totalCount, out object state)
            {
                for (int i = 0; i < _itemCount; i++)
                {
                    items.Add(new ArraySegment<Message>());
                }

                totalCount = _itemCount;
                state = null;
            }

            public override void WriteCursor(TextWriter textWriter)
            {
                throw new NotImplementedException();
            }

            private static IPerformanceCounterManager GetCounters()
            {
                var counters = new Mock<IPerformanceCounterManager>();
                counters.Setup(m => m.MessageBusSubscribersTotal)
                        .Returns(new NoOpPerformanceCounter());
                counters.Setup(m => m.MessageBusSubscribersCurrent)
                        .Returns(new NoOpPerformanceCounter());
                counters.Setup(m => m.MessageBusSubscribersPerSec)
                        .Returns(new NoOpPerformanceCounter());
                counters.Setup(m => m.MessageBusMessagesReceivedTotal)
                        .Returns(new NoOpPerformanceCounter());
                counters.Setup(m => m.MessageBusMessagesReceivedPerSec)
                        .Returns(new NoOpPerformanceCounter());

                return counters.Object;
            }
        }
    }
}
