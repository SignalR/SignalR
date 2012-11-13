// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
{
    internal static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = MakeTask<object>(null);
        private static readonly Task<bool> _trueTask = MakeTask<bool>(true);
        private static readonly Task<bool> _falseTask = MakeTask<bool>(false);

        private static Task<T> MakeTask<T>(T value)
        {
            return FromResult<T>(value);
        }

        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<bool> True
        {
            get
            {
                return _trueTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<bool> False
        {
            get
            {
                return _falseTask;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task OrEmpty(this Task task)
        {
            return task ?? Empty;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<T> OrEmpty<T>(this Task<T> task)
        {
            return task ?? TaskCache<T>.Empty;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state)
        {
            try
            {
                return Task.Factory.FromAsync(beginMethod, endMethod, state);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<T> FromAsync<T>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, T> endMethod, object state)
        {
            try
            {
                return Task.Factory.FromAsync<T>(beginMethod, endMethod, state);
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError<T>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static TTask Catch<TTask>(this TTask task) where TTask : Task
        {
            if (task != null && task.Status != TaskStatus.RanToCompletion)
            {
                task.ContinueWith(innerTask =>
                {
#if !WINDOWS_PHONE && !SILVERLIGHT && !NETFX_CORE
                    // observe Exception
                    var ex = innerTask.Exception;
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
#endif
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            return task;
        }

#if PERFCOUNTERS
        public static TTask Catch<TTask>(this TTask task, params IPerformanceCounter[] counters) where TTask : Task
        {
            return Catch(task, _ =>
                {
                    if (counters == null)
                    {
                        return;
                    }
                    for (var i = 0; i < counters.Length; i++)
                    {
                        counters[i].Increment();
                    }
                });
        }
#endif
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static TTask Catch<TTask>(this TTask task, Action<AggregateException> handler) where TTask : Task
        {
            if (task != null && task.Status != TaskStatus.RanToCompletion)
            {
                task.ContinueWith(innerTask =>
                {
                    var ex = innerTask.Exception;
                    // observe Exception
#if !WINDOWS_PHONE && !SILVERLIGHT && !NETFX_CORE
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
#endif
                    handler(ex);
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            return task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static void ContinueWithNotComplete(this Task task, TaskCompletionSource<object> tcs)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static void ContinueWith(this Task task, TaskCompletionSource<object> tcs)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            });
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static void ContinueWith<T>(this Task<T> task, TaskCompletionSource<T> tcs)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(t.Result);
                }
            });
        }

        /// <summary>
        /// Passes a task returning function into another task returning function so that
        /// it can decide when it starts and returns a task that completes when all are finished
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Interleave<T>(Func<T, Action, Task> before, Func<Task> after, T arg, TaskCompletionSource<object> tcs)
        {
            var tasks = new[] {
                            tcs.Task,
                            before(arg, () => after().ContinueWith(tcs))
                        };

            return tasks.Return();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Return(this Task[] tasks)
        {
            return Then(tasks, () => { });
        }

        // Then extesions
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then(this Task task, Action successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<TResult>(this Task task, Func<TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, TResult>.RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then(this Task[] tasks, Action successor)
        {
            if (tasks.Length == 0)
            {
                return FromMethod(successor);
            }

            var tcs = new TaskCompletionSource<object>();
            Task.Factory.ContinueWhenAll(tasks, completedTasks =>
            {
                var faulted = completedTasks.FirstOrDefault(t => t.IsFaulted);
                if (faulted != null)
                {
                    tcs.SetException(faulted.Exception);
                    return;
                }
                var cancelled = completedTasks.FirstOrDefault(t => t.IsCanceled);
                if (cancelled != null)
                {
                    tcs.SetCanceled();
                    return;
                }

                successor();
                tcs.SetResult(null);
            });

            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1>(this Task task, Action<T1> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, object, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1, T2>(this Task task, Action<T1, T2> successor, T1 arg1, T2 arg2)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2);

                default:
                    return GenericDelegates<object, object, T1, T2>.ThenWithArgs(task, successor, arg1, arg2);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1>(this Task task, Func<T1, Task> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1).FastUnwrap();

                default:
                    return GenericDelegates<object, Task, T1, object>.ThenWithArgs(task, successor, arg1)
                                                                     .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<T1, T2>(this Task task, Func<T1, T2, Task> successor, T1 arg1, T2 arg2)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2).FastUnwrap();

                default:
                    return GenericDelegates<object, Task, T1, T2>.ThenWithArgs(task, successor, arg1, arg2)
                                                                 .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, Task<TResult>> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result).FastUnwrap();

                default:
                    return TaskRunners<T, Task<TResult>>.RunTask(task, t => successor(t.Result))
                                                        .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<T, TResult>.RunTask(task, t => successor(t.Result));
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<T, T1, TResult>(this Task<T> task, Func<T, T1, TResult> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result, arg1);

                default:
                    return GenericDelegates<T, TResult, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then(this Task task, Func<Task> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor).FastUnwrap();

                default:
                    return TaskRunners<object, Task>.RunTask(task, successor)
                                                    .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<TResult>(this Task task, Func<Task<TResult>> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor).FastUnwrap();

                default:
                    return TaskRunners<object, Task<TResult>>.RunTask(task, successor)
                                                             .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<TResult>(this Task<TResult> task, Action<TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<TResult, object>.RunTask(task, successor);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Then<TResult>(this Task<TResult> task, Func<TResult, Task> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result).FastUnwrap();

                default:
                    return TaskRunners<TResult, Task>.RunTask(task, t => successor(t.Result))
                                                     .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<TResult> Then<TResult, T1>(this Task<TResult> task, Func<Task<TResult>, T1, Task<TResult>> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task, arg1).FastUnwrap();

                default:
                    return GenericDelegates<TResult, Task<TResult>, T1, object>.ThenWithArgs(task, successor, arg1)
                                                                               .FastUnwrap();
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task FastUnwrap(this Task<Task> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<T> FastUnwrap<T>(this Task<Task<T>> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Delay(TimeSpan timeOut)
        {
#if NETFX_CORE
            return Task.Delay(timeOut);
#else
            var tcs = new TaskCompletionSource<object>();

            var timer = new Timer(tcs.SetResult,
            null,
            timeOut,
            TimeSpan.FromMilliseconds(-1));

            return tcs.Task.ContinueWith(_ =>
            {
                timer.Dispose();
            },
            TaskContinuationOptions.ExecuteSynchronously);
#endif
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task AllSucceeded(this Task[] tasks, Action continuation)
        {
            return AllSucceeded(tasks, _ => continuation());
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task AllSucceeded(this Task[] tasks, Action<Task[]> continuation)
        {
            return Task.Factory.ContinueWhenAll(tasks, _ =>
            {
                var cancelledTask = tasks.FirstOrDefault(task => task.IsCanceled);
                if (cancelledTask != null)
                    throw new TaskCanceledException();

                var allExceptions =
                    tasks.Where(task => task.IsFaulted).SelectMany(task => task.Exception.InnerExceptions).ToList();

                if (allExceptions.Count > 0)
                {
                    throw new AggregateException(allExceptions);
                }

                continuation(tasks);
            });
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod(Action func)
        {
            try
            {
                func();
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1>(Action<T1> func, T1 arg)
        {
            try
            {
                func(arg);
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task FromMethod<T1, T2>(Action<T1, T2> func, T1 arg1, T2 arg2)
        {
            try
            {
                func(arg1, arg2);
                return Empty;
            }
            catch (Exception ex)
            {
                return FromError(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<TResult>(Func<TResult> func)
        {
            try
            {
                return FromResult<TResult>(func());
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<T1, TResult>(Func<T1, TResult> func, T1 arg)
        {
            try
            {
                return FromResult<TResult>(func(arg));
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        public static Task<TResult> FromMethod<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2)
        {
            try
            {
                return FromResult<TResult>(func(arg1, arg2));
            }
            catch (Exception ex)
            {
                return FromError<TResult>(ex);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static Task FromError(Exception e)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(e);
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        internal static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(e);
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static Task Canceled()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        private static Task<T> Canceled<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
        private static Task RunTask(Task task, Action successor)
        {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.SetException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    try
                    {
                        successor();
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
            });

            return tcs.Task;
        }

        private static class TaskRunners<T, TResult>
        {
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task RunTask(Task<T> task, Action<T> successor)
            {
                var tcs = new TaskCompletionSource<object>();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            successor(t.Result);
                            tcs.SetResult(null);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    }
                });

                return tcs.Task;
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task<TResult> RunTask(Task task, Func<TResult> successor)
            {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        tcs.SetException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            tcs.SetResult(successor());
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    }
                });

                return tcs.Task;
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are set in a tcs")]
            internal static Task<TResult> RunTask(Task<T> task, Func<Task<T>, TResult> successor)
            {
                var tcs = new TaskCompletionSource<TResult>();
                task.ContinueWith(t =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.SetException(t.Exception);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            tcs.SetResult(successor(t));
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    }
                });

                return tcs.Task;
            }
        }

        private static class GenericDelegates<T, TResult, T1, T2>
        {
            internal static Task ThenWithArgs(Task task, Action<T1> successor, T1 arg1)
            {
                return RunTask(task, () => successor(arg1));
            }

            internal static Task ThenWithArgs(Task task, Action<T1, T2> successor, T1 arg1, T2 arg2)
            {
                return RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task<TResult> ThenWithArgs(Task task, Func<T1, TResult> successor, T1 arg1)
            {
                return TaskRunners<object, TResult>.RunTask(task, () => successor(arg1));
            }

            internal static Task<TResult> ThenWithArgs(Task task, Func<T1, T2, TResult> successor, T1 arg1, T2 arg2)
            {
                return TaskRunners<object, TResult>.RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task<TResult> ThenWithArgs(Task<T> task, Func<T, T1, TResult> successor, T1 arg1)
            {
                return TaskRunners<T, TResult>.RunTask(task, t => successor(t.Result, arg1));
            }

            internal static Task<Task> ThenWithArgs(Task task, Func<T1, Task> successor, T1 arg1)
            {
                return TaskRunners<object, Task>.RunTask(task, () => successor(arg1));
            }

            internal static Task<Task> ThenWithArgs(Task task, Func<T1, T2, Task> successor, T1 arg1, T2 arg2)
            {
                return TaskRunners<object, Task>.RunTask(task, () => successor(arg1, arg2));
            }

            internal static Task<Task<TResult>> ThenWithArgs(Task<T> task, Func<T, T1, Task<TResult>> successor, T1 arg1)
            {
                return TaskRunners<T, Task<TResult>>.RunTask(task, t => successor(t.Result, arg1));
            }

            internal static Task<Task<T>> ThenWithArgs(Task<T> task, Func<Task<T>, T1, Task<T>> successor, T1 arg1)
            {
                return TaskRunners<T, Task<T>>.RunTask(task, t => successor(t, arg1));
            }
        }

        private static class TaskCache<T>
        {
            public static Task<T> Empty = MakeTask<T>(default(T));
        }
    }
}
