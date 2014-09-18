using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    // NOTE: All these tests rely heavily on the TaskQueue Funcs running
    // synchronously so that they are completed before the call to Enqueue completes.
    // If that ever changes, these tests will likely fail.
    public class TaskQueueFacts
    {
        [Fact]
        public void DoesNotNotifyMonitorOfInitialTask()
        {
            var mockMonitor = new Mock<ITaskMonitor>();
            var queue = new TaskQueue(TaskAsyncHelper.Empty, mockMonitor.Object);

            mockMonitor.Verify(m => m.TaskStarted(), Times.Never());
            mockMonitor.Verify(m => m.TaskCompleted(), Times.Never());
        }

        [Fact]
        public void NotifiesMonitorWhenTaskStartsAndCompletes()
        {
            var mockMonitor = new Mock<ITaskMonitor>();
            var queue = new TaskQueue(TaskAsyncHelper.Empty, mockMonitor.Object);

            queue.Enqueue(() => TaskAsyncHelper.Empty);

            mockMonitor.Verify(m => m.TaskStarted(), Times.Once());
            mockMonitor.Verify(m => m.TaskCompleted(), Times.Once());
        }


        [Fact]
        public void NotifiesMonitorWhenMultipleTasksStartsAndCompletes()
        {
            var mockMonitor = new Mock<ITaskMonitor>();
            var queue = new TaskQueue(TaskAsyncHelper.Empty, mockMonitor.Object);

            queue.Enqueue(() => TaskAsyncHelper.Empty);
            queue.Enqueue(() => TaskAsyncHelper.Empty);

            mockMonitor.Verify(m => m.TaskStarted(), Times.Exactly(2));
            mockMonitor.Verify(m => m.TaskCompleted(), Times.Exactly(2));
        }

        [Fact]
        public void DoesNotNotifyMonitorOfCompletionUntilFuncReturns()
        {
            var mockMonitor = new Mock<ITaskMonitor>();
            var queue = new TaskQueue(TaskAsyncHelper.Empty, mockMonitor.Object);

            queue.Enqueue(() =>
            {
                mockMonitor.Verify(m => m.TaskStarted(), Times.Once());
                mockMonitor.Verify(m => m.TaskCompleted(), Times.Never());
                return TaskAsyncHelper.Empty;
            });

            mockMonitor.Verify(m => m.TaskStarted(), Times.Once());
            mockMonitor.Verify(m => m.TaskCompleted(), Times.Once());
        }


        [Fact]
        public void DoesNotNotifyMonitorOfCompletionUntilTaskCompletesReturns()
        {
            var mockMonitor = new Mock<ITaskMonitor>();
            var queue = new TaskQueue(TaskAsyncHelper.Empty, mockMonitor.Object);

            var tcs = new TaskCompletionSource<object>();

            queue.Enqueue(() =>
            {
                mockMonitor.Verify(m => m.TaskStarted(), Times.Once());
                return tcs.Task;
            });

            mockMonitor.Verify(m => m.TaskCompleted(), Times.Never());

            tcs.SetResult(null);

            mockMonitor.Verify(m => m.TaskStarted(), Times.Once());
            mockMonitor.Verify(m => m.TaskCompleted(), Times.Once());
        }
    }
}
