#if __ANDROID__
// https://github.com/mono/mono/blob/master/mcs/class/System.Core/System.Threading.Tasks/TaskExtensions.cs
namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        const TaskContinuationOptions opt = TaskContinuationOptions.ExecuteSynchronously;

        public static Task<TResult> Unwrap<TResult>(this Task<Task<TResult>> task)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            TaskCompletionSource<TResult> src = new TaskCompletionSource<TResult>();

            task.ContinueWith(t1 => CopyCat(t1, src, () => t1.Result.ContinueWith(t2 => CopyCat(t2, src, () => src.SetResult(t2.Result)), opt)), opt);

            return src.Task;
        }

        public static Task Unwrap(this Task<Task> task)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            TaskCompletionSource<object> src = new TaskCompletionSource<object>();

            task.ContinueWith(t1 => CopyCat(t1, src, () => t1.Result.ContinueWith(t2 => CopyCat(t2, src, () => src.SetResult(null)), opt)), opt);

            return src.Task;
        }

        static void CopyCat<TResult>(Task source,
                                      TaskCompletionSource<TResult> dest,
                                      Action normalAction)
        {
            if (source.IsCanceled)
                dest.SetCanceled();
            else if (source.IsFaulted)
                dest.SetException(source.Exception.InnerExceptions);
            else
                normalAction();
        }
    }
}
#endif