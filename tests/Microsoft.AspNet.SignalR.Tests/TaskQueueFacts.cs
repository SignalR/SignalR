using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class TaskQueueFacts
    {
        [Fact]
        public void DrainingTaskQueueShutsQueueOff()
        {
            var queue = new TaskQueue();
            queue.Enqueue(() => TaskAsyncHelper.Empty);
            queue.Drain();
            Task task = queue.Enqueue(() => TaskAsyncHelper.FromError(new Exception()));

            Assert.True(task.IsCompleted);
            Assert.False(task.IsFaulted);
        }

        [Fact]
        public void TaskQueueDoesNotQueueNewTasksIfPreviousTaskFaulted()
        {
            var queue = new TaskQueue();
            queue.Enqueue(() => TaskAsyncHelper.FromError(new Exception()));
            Task task = queue.Enqueue(() => TaskAsyncHelper.Empty);

            Assert.True(task.IsCompleted);
            Assert.True(task.IsFaulted);
        }

        [Fact]
        public void TaskQueueRunsTasksInSequence()
        {
            var queue = new TaskQueue();
            int n = 0;
            queue.Enqueue(() =>
            {
                n++;
                return TaskAsyncHelper.Empty;
            });

            Task task = queue.Enqueue(() =>
            {
                return Task.Delay(100).Then(() => n++);
            });

            task.Wait();
            Assert.Equal(n, 2);
        }

        [Fact]
        public void FailedToEnqueueReturnsNull()
        {
            var queue = new TaskQueue(TaskAsyncHelper.Empty, 2);
            queue.Enqueue(() => Task.Delay(100));
            queue.Enqueue(() => Task.Delay(100));
            Task task = queue.Enqueue(() => Task.Delay(100));
            Assert.Null(task);
        }
    }
}
