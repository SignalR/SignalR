using System;
using System.Diagnostics;
using System.Linq;
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

        public static Task Catch(this Task task)
        {
            return task.ContinueWith(t =>
            {
                if (t != null && t.IsFaulted)
                {
                    var ex = t.Exception;
#if !WINDOWS_PHONE
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
#endif
                }
                return t;
            }).Unwrap();
        }

        public static Task<T> Catch<T>(this Task<T> task)
        {
            return task.ContinueWith(t =>
            {
                if (t != null && t.IsFaulted)
                {
                    var ex = t.Exception;
#if !WINDOWS_PHONE
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
#endif
                }
                return t;
            })
            .Unwrap();
        }

        public static Task Success(this Task task, Action<Task> successor)
        {
            return task.ContinueWith(_ =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    return task;
                }
                return Task.Factory.StartNew(() => successor(task));
            }).Unwrap();
        }

        public static Task Success<TResult>(this Task<TResult> task, Action<Task<TResult>> successor)
        {
            return task.ContinueWith(_ =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    return task;
                }
                return Task.Factory.StartNew(() => successor(task));
            }).Unwrap();
        }

        public static Task<TResult> Success<TResult>(this Task task, Func<Task, TResult> successor)
        {
            return task.ContinueWith(_ =>
            {
                if (task.IsFaulted)
                {
                    return FromError<TResult>(task.Exception);
                }
                if (task.IsCanceled)
                {
                    return Cancelled<TResult>();
                }
                return Task.Factory.StartNew(() => successor(task));
            }).Unwrap();
        }

        public static Task<TResult> Success<T, TResult>(this Task<T> task, Func<Task<T>, TResult> successor)
        {
            return task.ContinueWith(_ =>
            {
                if (task.IsFaulted)
                {
                    return FromError<TResult>(task.Exception);
                }
                if (task.IsCanceled)
                {
                    return Cancelled<TResult>();
                }
                return Task.Factory.StartNew(() => successor(task));
            }).Unwrap();
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

                return Task.Factory.StartNew(() => continuation(tasks));

            }).Unwrap();
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

                return Task.Factory.StartNew(continuation);

            }).Unwrap();
        }

        public static Task<T> FromResult<T>(T value)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        private static Task<T> FromError<T>(Exception e)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(e);
            return tcs.Task;
        }

        private static Task<T> Cancelled<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs.Task;
        }
    }
}