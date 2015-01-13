using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR
{
    internal static class TaskAwaiterHelper
    {
        internal static PreserveCultureAwaiter PreserveCulture(this Task task)
        {
            return new PreserveCultureAwaiter(task, useSyncContext: true);
        }

        internal static PreserveCultureAwaiter PreserveCultureNotContext(this Task task)
        {
            return new PreserveCultureAwaiter(task, useSyncContext: false);
        }

        internal static PreserveCultureAwaiter<T> PreserveCulture<T>(this Task<T> task)
        {
            return new PreserveCultureAwaiter<T>(task, useSyncContext: true);
        }

        internal static PreserveCultureAwaiter<T> PreserveCultureNotContext<T>(this Task<T> task)
        {
            return new PreserveCultureAwaiter<T>(task, useSyncContext: false);
        }

        private static void PreserveCultureUnsafeOnCompleted(ICriticalNotifyCompletion notifier,
                                                             Action continuation,
                                                             bool useSyncContext)
        {
            // Rely on the SyncContext to preserve culture if it exists
            if (useSyncContext && SynchronizationContext.Current != null)
            {
                notifier.UnsafeOnCompleted(continuation);
            }
            else
            {
                var preservedCulture = TaskAsyncHelper.SaveCulture();
                notifier.UnsafeOnCompleted(() =>
                {
                    TaskAsyncHelper.RunWithPreservedCulture(preservedCulture, continuation);
                });
            }
        }

        internal struct PreserveCultureAwaiter : ICriticalNotifyCompletion
        {
            private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _awaiter;
            private readonly bool _useSyncContext;

            public PreserveCultureAwaiter(Task task, bool useSyncContext)
            {
                _awaiter = task.ConfigureAwait(useSyncContext).GetAwaiter();
                _useSyncContext = useSyncContext;
            }

            public bool IsCompleted
            {
                get { return _awaiter.IsCompleted; }
            }

            public void OnCompleted(Action continuation)
            {
                throw new NotImplementedException();
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                PreserveCultureUnsafeOnCompleted(_awaiter, continuation, _useSyncContext);
            }

            public void GetResult()
            {
                _awaiter.GetResult();
            }

            public PreserveCultureAwaiter GetAwaiter()
            {
                return this;
            }
        }

        internal struct PreserveCultureAwaiter<T> : ICriticalNotifyCompletion
        {
            private readonly ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter _awaiter;
            private readonly bool _useSyncContext;

            public PreserveCultureAwaiter(Task<T> task, bool useSyncContext)
            {
                _awaiter = task.ConfigureAwait(useSyncContext).GetAwaiter();
                _useSyncContext = useSyncContext;
            }

            public bool IsCompleted
            {
                get { return _awaiter.IsCompleted; }
            }

            public void OnCompleted(Action continuation)
            {
                throw new NotImplementedException();
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                PreserveCultureUnsafeOnCompleted(_awaiter, continuation, _useSyncContext);
            }

            public T GetResult()
            {
                return _awaiter.GetResult();
            }

            public PreserveCultureAwaiter<T> GetAwaiter()
            {
                return this;
            }
        }
    }
}
