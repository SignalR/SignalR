using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class TaskAsyncHelperFacts
    {
        [Fact]
        public void TaskAsyncHelpersPreserveCulture()
        {
            TaskCompletionSource<CultureInfo> tcs = null;
            TaskCompletionSource<CultureInfo> uiTcs = null;
            var defaultCulture = Thread.CurrentThread.CurrentCulture;
            var defaultUiCulture = Thread.CurrentThread.CurrentUICulture;
            var testCulture = new CultureInfo("zh-Hans");
            var testUICulture = new CultureInfo("zh-CN");

            Func<Task> saveThreadCulture = () =>
            {
                tcs.SetResult(Thread.CurrentThread.CurrentCulture);
                uiTcs.SetResult(Thread.CurrentThread.CurrentUICulture);
                return TaskAsyncHelper.Empty;
            };

            Action<IEnumerable<Func<Task<object>>>, Action<Task<object>>> ensureCulturePreserved = (taskGenerators, testAction) =>
            {
                foreach (var taskGenerator in taskGenerators)
                {
                    tcs = new TaskCompletionSource<CultureInfo>();
                    uiTcs = new TaskCompletionSource<CultureInfo>();
                    testAction(taskGenerator());
                    Assert.Equal(testCulture, tcs.Task.Result);
                    Assert.Equal(testUICulture, uiTcs.Task.Result);
                }
            };

            try
            {
                Thread.CurrentThread.CurrentCulture = testCulture;
                Thread.CurrentThread.CurrentUICulture = testUICulture;

                var successfulTaskGenerators = new Func<Task<object>>[]
                {
                    () => TaskAsyncHelper.FromResult<object>(null),                                          // Completed
                    () => TaskAsyncHelper.Delay(TimeSpan.FromMilliseconds(50)).Then(() => (object)null),     // Async Completed
                };

                // Non-generic Then with sync/async completed tasks
                ensureCulturePreserved(successfulTaskGenerators, task => task.Then(saveThreadCulture));

                var faultedTcs = new TaskCompletionSource<object>();
                var canceledTcs = new TaskCompletionSource<object>();
                faultedTcs.SetException(new Exception());
                canceledTcs.SetCanceled();
                var allTaskGenerators = successfulTaskGenerators.Concat(new Func<Task<object>>[]
                {
                    () => faultedTcs.Task,                                                                   // Faulted
                    () => canceledTcs.Task,                                                                  // Canceled
                    () => TaskAsyncHelper.Delay(TimeSpan.FromMilliseconds(50)).Then(() => faultedTcs.Task),  // Async Faulted
                    () => TaskAsyncHelper.Delay(TimeSpan.FromMilliseconds(50)).Then(() => canceledTcs.Task), // Async Canceled
                });

                // Generic ContinueWithPreservedCulture with sync/async faulted, canceled and completed tasks
                ensureCulturePreserved(allTaskGenerators, task => task.ContinueWithPreservedCulture(_ => saveThreadCulture()));

                // Verify that threads in the ThreadPool keep the default culture
                tcs = new TaskCompletionSource<CultureInfo>();
                uiTcs = new TaskCompletionSource<CultureInfo>();
                TaskAsyncHelper.Delay(TimeSpan.FromMilliseconds(100)).ContinueWith(_ => saveThreadCulture());
                Assert.Equal(defaultCulture, tcs.Task.Result);
                Assert.Equal(defaultUiCulture, uiTcs.Task.Result);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = defaultCulture;
                Thread.CurrentThread.CurrentUICulture = defaultUiCulture;
            }
        }
    }
}
