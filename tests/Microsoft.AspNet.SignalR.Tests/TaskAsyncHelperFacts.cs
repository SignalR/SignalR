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
        private static readonly CultureInfo _defaultCulture = Thread.CurrentThread.CurrentCulture;
        private static readonly CultureInfo _defaultUICulture = Thread.CurrentThread.CurrentUICulture;
        private static readonly CultureInfo _testCulture = new CultureInfo("zh-Hans");
        private static readonly CultureInfo _testUICulture = new CultureInfo("zh-CN");

        private static readonly Func<Task>[] _successfulTaskGenerators = new Func<Task>[]
        {
            () => TaskAsyncHelper.FromResult<object>(null), // Sync Completed
            async () => await Task.Yield(), // Async Completed
        };

        private static readonly Func<Task>[] _failedTaskGenerators = new Func<Task>[]
        {
            () =>
            {
                var faultedTcs = new TaskCompletionSource<object>();
                faultedTcs.SetException(new Exception());
                return faultedTcs.Task; // Sync Faulted
            },
            () =>
            {
                var canceledTcs = new TaskCompletionSource<object>();
                canceledTcs.SetCanceled();
                return canceledTcs.Task; // Sync Canceled
            },
            async () => 
            {
                await Task.Yield();
                throw new Exception();
            },  // Async Faulted
            async () =>
            {
                await Task.Yield();
                throw new OperationCanceledException();
            } // Async Canceled
        };

        private void EnsureCulturePreserved(IEnumerable<Func<Task>> taskGenerators, Action<Task, Action> testAction)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = _testCulture;
                Thread.CurrentThread.CurrentUICulture = _testUICulture;

                TaskCompletionSource<CultureInfo> cultureTcs = null;
                TaskCompletionSource<CultureInfo> uiCultureTcs = null;

                Action initialize = () =>
                {
                    cultureTcs = new TaskCompletionSource<CultureInfo>();
                    uiCultureTcs = new TaskCompletionSource<CultureInfo>();
                };

                Action saveCulture = () =>
                {
                    cultureTcs.SetResult(Thread.CurrentThread.CurrentCulture);
                    uiCultureTcs.SetResult(Thread.CurrentThread.CurrentUICulture);
                };

                foreach (var taskGenerator in taskGenerators)
                {
                    initialize();

                    testAction(taskGenerator(), saveCulture);

                    Assert.Equal(_testCulture, cultureTcs.Task.Result);
                    Assert.Equal(_testUICulture, uiCultureTcs.Task.Result);
                }

                // Verify that threads in the ThreadPool keep the default culture
                initialize();

                Task.Delay(100).ContinueWith(_ => saveCulture());

                Assert.Equal(_defaultCulture, cultureTcs.Task.Result);
                Assert.Equal(_defaultUICulture, uiCultureTcs.Task.Result);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = _defaultCulture;
                Thread.CurrentThread.CurrentUICulture = _defaultUICulture;
            }
        }

        [Fact]
        public void ThenPreservesCulture()
        {
            // Then with sync/async completed tasks
            EnsureCulturePreserved(_successfulTaskGenerators,
                (task, continuation) => task.Then(continuation));
        }

        [Fact]
        public void ContinuePreservedCulturePreservesCulture()
        {
            // ContinueWithPreservedCulture with sync/async faulted, canceled and completed tasks
            EnsureCulturePreserved(_successfulTaskGenerators.Concat(_failedTaskGenerators),
                (task, continuation) => task.ContinueWithPreservedCulture(_ => continuation()));
        }

        [Fact]
        public void PreserveCultureAwaiterPreservesCulture()
        {
            // PreserveCultureAwaiter with sync/async faulted, canceled and completed tasks
            EnsureCulturePreserved(_successfulTaskGenerators.Concat(_failedTaskGenerators),
                async (task, continuation) =>
                {
                    try
                    {
                        await task.PreserveCulture();
                    }
                    catch
                    {
                        // The MSBuild xUnit.net runner crashes if we don't catch here
                    }
                    finally
                    {
                        continuation();
                    }
                });
        }
    }
}
