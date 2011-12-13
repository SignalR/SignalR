using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

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

        public static Task Then(this Task task, Action<Task> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled();

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

        public static Task<TResult> Then<TResult>(this Task task, Func<Task, TResult> successor)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    return FromError<TResult>(task.Exception);

                case TaskStatus.Canceled:
                    return Canceled<TResult>();

                case TaskStatus.RanToCompletion:
                    return FromMethod<TResult, Task>(successor, task);

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
                    return FromMethod<TResult, Task<T>>(successor, task);

                default:
                    return task.ContinueWith(t => successor(t), TaskContinuationOptions.OnlyOnRanToCompletion);
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

        public static Task<T> AllSucceeded<T>(this Task[] tasks, Func<T> continuation)
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

                return continuation();
            });
        }

        public static Task FromMethod<T>(Action<T> func, T arg)
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

        public static Task<T> FromMethod<T>(Func<T> func)
        {
            try
            {
                return FromResult<T>(func());
            }
            catch (Exception ex)
            {
                return FromError<T>(ex);
            }
        }

        public static Task<TResult> FromMethod<TResult, TArg>(Func<TArg, TResult> func, TArg arg)
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

        public static Task<TResult> FromMethod<TResult, T1, T2>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2)
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

        private static Task FromError(Exception e)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(e);
            return tcs.Task;
        }

        private static Task<T> FromError<T>(Exception e)
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
    }
}