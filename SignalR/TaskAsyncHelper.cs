using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR {
    internal static class TaskAsyncHelper {
        private static Task _empty = MakeEmpty();

        private static Task MakeEmpty() {
            return FromResult<object>(null);
        }

        public static Task Empty { get { return _empty; } }

        public static Task Catch(this Task task) {
            return task.ContinueWith(t => {
                if (t != null && t.IsFaulted) {
                    var ex = t.Exception;
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
                }
                return t;
            }).Unwrap();
        }

        public static Task<T> Catch<T>(this Task<T> task) {
            return task.ContinueWith(t => {
                if (t != null && t.IsFaulted) {
                    var ex = t.Exception;
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
                }
                return t;
            })
            .Unwrap();
        }

        public static Task Success(this Task task, Action<Task> successor) {
            return task.ContinueWith(_ =>
            {
            	if (task.IsCanceled || task.IsFaulted)
                {
                	return task;
                }
            	return Task.Factory.StartNew(() => successor(task));
            }).Unwrap();
        }

        public static Task Success<TResult>(this Task<TResult> task, Action<Task<TResult>> successor) {
			return task.ContinueWith(_ =>
			{
				if (task.IsCanceled || task.IsFaulted)
				{
					return task;
				}
				return Task.Factory.StartNew(() => successor(task));
			}).Unwrap();
        }

        public static Task<TResult> Success<TResult>(this Task task, Func<Task, TResult> successor) {
			return task.ContinueWith(_ =>
			{
				if (task.IsFaulted)
				{
					return FromError<TResult>(task.Exception);
				}
				if(task.IsCanceled)
				{
					return Cancelled<TResult>();
				}
				return Task.Factory.StartNew(() => successor(task));
			}).Unwrap();
        }

        public static Task<TResult> Success<T, TResult>(this Task<T> task, Func<Task<T>, TResult> successor) {
            var tcs = new TaskCompletionSource<TResult>();
            task.ContinueWith(_ => {
                if (task.IsCanceled) {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted) {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else {
                    try {
                        tcs.TrySetResult(successor(task));
                    }
                    catch (Exception ex) {
                        tcs.TrySetException(ex);
                    }
                }
            });
            return tcs.Task;
        }

        public static Task AllSucceeded(this Task[] tasks, Action continuation) {
            var tcs = new TaskCompletionSource<object>();
            Task.Factory.ContinueWhenAll(tasks, _ => {
                var allExceptions = tasks.Where(task => task.IsFaulted || task.IsCanceled).SelectMany(task => task.Exception.InnerExceptions).ToList();

                if (allExceptions.Count > 0) {
                    tcs.TrySetException(allExceptions);
                }
                else {
                    try {
                        continuation();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex) {
                        tcs.TrySetException(ex);
                    }
                }
            });
            return tcs.Task;
        }

        public static Task AllSucceeded(this Task[] tasks, Action<Task[]> continuation) {
            var tcs = new TaskCompletionSource<object>();
            Task.Factory.ContinueWhenAll(tasks, _ => {
                var allExceptions = tasks.Where(task => task.IsFaulted || task.IsCanceled).SelectMany(task => task.Exception.InnerExceptions).ToList();

                if (allExceptions.Count > 0) {
                    tcs.TrySetException(allExceptions);
                }
                else {
                    try {
                        continuation(tasks);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex) {
                        tcs.TrySetException(ex);
                    }
                }
            });
            return tcs.Task;
        }

        public static Task<T> AllSucceeded<T>(this Task[] tasks, Func<T> continuation) {
            var tcs = new TaskCompletionSource<T>();
            Task.Factory.ContinueWhenAll(tasks, _ => {
                var allExceptions = tasks.Where(task => task.IsFaulted || task.IsCanceled).SelectMany(task => task.Exception.InnerExceptions).ToList();

                if (allExceptions.Count > 0) {
                    tcs.TrySetException(allExceptions);
                }
                else {
                    try {
                        tcs.TrySetResult(continuation());
                    }
                    catch (Exception ex) {
                        tcs.TrySetException(ex);
                    }
                }
            });
            return tcs.Task;
        }

        public static Task<T> FromResult<T>(T value) {
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