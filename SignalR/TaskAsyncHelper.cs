using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SignalR
{
    internal static class TaskAsyncHelper
    {
        private static readonly Task _emptyTask = MakeEmpty();

        private static Task MakeEmpty()
        {
            return FromResult<object>(null);
        }

        public static Task Empty
        {
            get
            {
                return _emptyTask;
            }
        }

        public static TTask Catch<TTask>(this TTask task) where TTask : Task
        {
            if (task != null && task.Status != TaskStatus.RanToCompletion)
            {
                task.ContinueWith(innerTask =>
                {
                    var ex = innerTask.Exception;
                    // observe Exception
#if !WINDOWS_PHONE && !SILVERLIGHT
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
#endif
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            return task;
        }

        #region Legacy Task.Then() extensions, don't use these

        public static Task<TResult> Then<TResult>(this Task task, Func<Task, TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task);

                default:
                    return task.ContinueWith(t => successor(t), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        public static Task<TResult> Then<T, TResult>(this Task<T> task, Func<Task<T>, TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task);

                default:
                    return task.ContinueWith(t => successor(t), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        public static Task Then<TResult>(this Task<TResult> task, Action<Task<TResult>> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task);

                default:
                    return task.ContinueWith(t => successor(t), TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        #endregion

        public static void ContinueWith(this Task task, TaskCompletionSource<object> tcs)
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
                else
                {
                    tcs.SetResult(null);
                }
            });
        }

        // Then extesions
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

        public static Task Then<T1>(this Task task, Action<T1> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, object, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        public static Task Then<T1, T2>(this Task task, Action<T1, T2> successor, T1 arg1, T2 arg2)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2);

                default:
                    return GenericDelegates<object, object, T1, T2>.ThenWithArgs(task, successor, arg1, arg2);
            }
        }


        public static Task<TResult> Then<T1, TResult>(this Task task, Func<T1, TResult> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, TResult, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        public static Task<TResult> Then<T1, T2, TResult>(this Task task, Func<T1, T2, TResult> successor, T1 arg1, T2 arg2)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1, arg2);

                default:
                    return GenericDelegates<object, TResult, T1, T2>.ThenWithArgs(task, successor, arg1, arg2);
            }
        }


        public static Task<T> Then<T, T1>(this Task<T> task, Func<T, T1, T> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<T>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<T>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result, arg1);

                default:
                    return GenericDelegates<T, T, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }


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


        public static Task<Task> Then(this Task task, Func<Task> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, Task>.RunTask(task, successor);
            }
        }

        public static Task<Task<TResult>> Then<TResult>(this Task task, Func<Task<TResult>> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task<TResult>>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task<TResult>>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor);

                default:
                    return TaskRunners<object, Task<TResult>>.RunTask(task, successor);
            }
        }

        public static Task<Task> Then<T>(this Task<T> task, Func<T, Task> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result);

                default:
                    return TaskRunners<T, Task>.RunTask(task, t => successor(t.Result));
            }
        }

        public static Task<Task> Then<T1>(this Task task, Func<T1, Task> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, arg1);

                default:
                    return GenericDelegates<object, Task, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        public static Task<Task<TResult>> Then<T, T1, TResult>(this Task<T> task, Func<T, T1, Task<TResult>> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task<TResult>>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task<TResult>>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task.Result, arg1);

                default:
                    return GenericDelegates<T, Task<TResult>, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        public static Task<Task<T>> Then<T, T1>(this Task<T> task, Func<Task<T>, T1, Task<T>> successor, T1 arg1)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<Task<T>>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<Task<T>>();

                case TaskStatus.RanToCompletion:
                    return FromMethod(successor, task, arg1);

                default:
                    return GenericDelegates<T, Task<T>, T1, object>.ThenWithArgs(task, successor, arg1);
            }
        }

        public static Task FastUnwrap(this Task<Task> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        public static Task<T> FastUnwrap<T>(this Task<Task<T>> task)
        {
            var innerTask = (task.Status == TaskStatus.RanToCompletion) ? task.Result : null;
            return innerTask ?? task.Unwrap();
        }

        public static Task AllSucceeded(this Task[] tasks, Action continuation)
        {
            return AllSucceeded(tasks, _ => continuation());
        }

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

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        public static TaskContinueWithMethod GetContinueWith(Type taskType)
        {
            var continueWith = (from m in taskType.GetMethods()
                                let methodParameters = m.GetParameters()
                                where m.Name.Equals("ContinueWith", StringComparison.OrdinalIgnoreCase) &&
                                    methodParameters.Length == 1
                                let parameter = methodParameters[0]
                                where parameter.ParameterType.IsGenericType &&
                                typeof(Func<,>) == parameter.ParameterType.GetGenericTypeDefinition()
                                select new TaskContinueWithMethod
                                {
                                    Method = m.MakeGenericMethod(typeof(Task)),
                                    Type = parameter.ParameterType.GetGenericArguments()[0]
                                })
                .FirstOrDefault();
            return continueWith;
        }

        internal static Task FromError(Exception e)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(e);
            return tcs.Task;
        }

        internal static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(e);
            return tcs.Task;
        }

        private static Task Canceled()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        private static Task<T> Canceled<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs.Task;
        }

        internal class TaskContinueWithMethod
        {
            public MethodInfo Method { get; set; }
            public Type Type { get; set; }
        }

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

            internal static Task<Task<TResult>> ThenWithArgs(Task<T> task, Func<T, T1, Task<TResult>> successor, T1 arg1)
            {
                return TaskRunners<T, Task<TResult>>.RunTask(task, t => successor(t.Result, arg1));
            }

            internal static Task<Task<T>> ThenWithArgs(Task<T> task, Func<Task<T>, T1, Task<T>> successor, T1 arg1)
            {
                return TaskRunners<T, Task<T>>.RunTask(task, t => successor(t, arg1));
            }
        }
    }
}