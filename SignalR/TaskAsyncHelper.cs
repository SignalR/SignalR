using System;
using System.Threading.Tasks;

namespace SignalR {
    public static class TaskAsyncHelper {
        private static Task _empty = MakeEmpty();
        private static Task MakeEmpty() {
            return FromResult<object>(null);
        }

        public static Task Empty { get { return _empty; } }

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