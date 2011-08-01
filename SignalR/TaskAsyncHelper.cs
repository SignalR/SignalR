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
                    var ex  = t.Exception;
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
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(_ => {
                if (task.IsCanceled) {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted) {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else {
                    try {
                        successor(task);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex) {
                        tcs.TrySetException(ex);
                    }
                }
            });
            return tcs.Task;
        }

        public static Task Success<TResult>(this Task<TResult> task, Action<Task<TResult>> successor) {
            var tcs = new TaskCompletionSource<object>();
            task.ContinueWith(_ => {
                if (task.IsCanceled) {
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted) {
                    tcs.TrySetException(task.Exception.InnerExceptions);
                }
                else {
                    try {
                        successor(task);
                        tcs.TrySetResult(null);
                    }
                    catch (Exception ex) {
                        tcs.TrySetException(ex);
                    }
                }
            });
            return tcs.Task;
        }

        public static Task<TResult> Success<TResult>(this Task task, Func<Task, TResult> successor) {
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

        public static IAsyncResult BeginTask(Func<Task> taskFunc, AsyncCallback callback, object state) {
            Task task = taskFunc();
            if (task == null) {
                return null;
            }

            var retVal = new TaskWrapperAsyncResult(task, state);

            if (callback != null) {
                // The callback needs the same argument that the Begin method returns, which is our special wrapper, not the original Task.
                task.ContinueWith(_ => callback(retVal));
            }

            return retVal;
        }

        public static void EndTask(IAsyncResult ar) {
            if (ar == null) {
                throw new ArgumentNullException("ar");
            }

            // The End* method doesn't actually perform any actual work, but we do need to maintain two invariants:
            // 1. Make sure the underlying Task actually *is* complete.
            // 2. If the Task encountered an exception, observe it here.
            // (The Wait method handles both of those.)
            var castResult = (TaskWrapperAsyncResult)ar;
            castResult.Task.Wait();
        }
    }
}