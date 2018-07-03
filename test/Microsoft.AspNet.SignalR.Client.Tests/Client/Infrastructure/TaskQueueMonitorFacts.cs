// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class TaskQueueMonitorFacts
    {
        private static readonly string _expectedErrorMessage =
            String.Format(CultureInfo.CurrentCulture,
                          Resources.Error_PossibleDeadlockDetected,
                          Timeout.InfiniteTimeSpan.TotalSeconds);

        [Fact]
        public void ErrorsAreTriggeredForLongRunningTasks()
        {
            VerifyErrorCount(Times.Once(), monitor =>
            {
                monitor.TaskStarted();

                monitor.Beat();
                monitor.Beat();
            });
        }

        [Fact]
        public void ErrorsAreNotTriggeredMultipleTimesForTheSameTask()
        {
            VerifyErrorCount(Times.Once(), monitor =>
            {
                monitor.TaskStarted();

                monitor.Beat();
                monitor.Beat();
                monitor.Beat();
            });
        }

        [Fact]
        public void MultipleErrorsAreTriggeredForMultipleLongRunningTasks()
        {
            VerifyErrorCount(Times.Exactly(2), monitor =>
            {
                monitor.TaskStarted();

                monitor.Beat();
                monitor.Beat();

                monitor.TaskCompleted();
                monitor.TaskStarted();

                monitor.Beat();
                monitor.Beat();
            });
        }

        [Fact]
        public void ErrorsAreNotTriggeredBeforeATaskStarts()
        {
            VerifyErrorCount(Times.Never(), monitor =>
            {
                monitor.Beat();
                monitor.Beat();
            });
        }

        [Fact]
        public void ErrorsAreNotTriggeredForShortRunningTasks()
        {
            VerifyErrorCount(Times.Never(), monitor =>
            {
                monitor.TaskStarted();

                monitor.Beat();

                monitor.TaskCompleted();
                monitor.TaskStarted();
                monitor.TaskCompleted();
                monitor.TaskStarted();

                monitor.Beat();

                monitor.TaskCompleted();

                monitor.Beat();
                monitor.Beat();
            });
        }

        [Fact]
        public async Task ErrorsAreTriggeredByTimer()
        {
            var mockConnection = new Mock<IConnection>();
            var wh = new TaskCompletionSource<object>();

            mockConnection.Setup(c => c.OnError(It.IsAny<SlowCallbackException>())).Callback(() => wh.TrySetResult(null));

            using (var monitor = new TaskQueueMonitor(mockConnection.Object, TimeSpan.FromMilliseconds(100)))
            {
                monitor.TaskStarted();
                await wh.Task.OrTimeout();
            };
        }

        [Fact]
        public void ErrorsAreNotTriggeredByTimerAfterDisposal()
        {
            var mockConnection = new Mock<IConnection>();
            var wh = new ManualResetEventSlim();

            mockConnection.Setup(c => c.OnError(It.IsAny<SlowCallbackException>())).Callback(wh.Set);

            using (var monitor = new TaskQueueMonitor(mockConnection.Object, TimeSpan.FromMilliseconds(100)))
            {
                monitor.TaskStarted();
            };

            Assert.False(wh.Wait(TimeSpan.FromMilliseconds(500)));
        }

        private static void VerifyErrorCount(Times count, Action<TaskQueueMonitor> test)
        {
            var connection = Mock.Of<IConnection>();

            using (var monitor = new TaskQueueMonitor(connection, Timeout.InfiniteTimeSpan))
            {
                test(monitor);
            }

            Mock.Get(connection)
                .Verify(c => c.OnError(It.Is<SlowCallbackException>(e => e.Message == _expectedErrorMessage)), count);
        }
    }
}
