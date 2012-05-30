using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Serialization;

namespace SignalR.Client.Net20.Infrastructure
{
    /// <summary>
    /// Represents a non-generic task that will be executed eventually.
    /// </summary>
    [ComVisible(false)]
    public class Task : Task<object>
    {
    }

    /// <summary>
    /// Represents a task that will be executed eventually.
    /// </summary>
    /// <typeparam name="T">The type of result from this task.</typeparam>
    public class Task<T>
    {
        /// <summary>
        /// The event that is called when this task is finised.
        /// </summary>
        public event EventHandler<CustomResultArgs<T>> OnFinish;

        /// <summary>
        /// Call the given function when this task is finished.
        /// </summary>
        /// <typeparam name="TFollowing">The return type of the given function.</typeparam>
        /// <param name="nextAction">The function that will be called when this task is finished.</param>
        /// <returns>A task that will return the given result type.</returns>
        public Task<TFollowing> Then<TFollowing>(Func<T,TFollowing> nextAction)
        {
            var nextEventTask = new Task<TFollowing>();
            OnFinish += (sender, e) =>
                            {
                                //Fail fast here. Need to evaluate the appropriate action in this case.
                                if (e.ResultWrapper.IsFaulted)
                                {
                                    throw e.ResultWrapper.Exception;
                                }
                                nextEventTask.OnFinished(nextAction(e.ResultWrapper.Result), e.ResultWrapper.Exception);
                            };
            return nextEventTask;
        }

        /// <summary>
        /// Call the given function when this task is finished.
        /// </summary>
        /// <param name="nextAction">The function that will be called when this task is finished.</param>
        /// <returns>A non-generic task.</returns>
        public Task Then(Action<T> nextAction)
        {
            var nextEventTask = new Task();
            OnFinish += (sender, e) =>
                            {
                                nextAction(e.ResultWrapper.Result);
                                nextEventTask.OnFinished(null, e.ResultWrapper.Exception);
                            };
            return nextEventTask;
        }

        /// <summary>
        /// Continue with the given function when this task is finished.
        /// </summary>
        /// <param name="nextAction">The function that will be called when this task is finished.</param>
        /// <returns>A non-generic task.</returns>
        public Task ContinueWith(Action<ResultWrapper<T>> nextAction)
        {
            var nextEventTask = new Task();
            OnFinish += (sender, e) =>
            {
                nextAction(e.ResultWrapper);
                nextEventTask.OnFinished(null, e.ResultWrapper.Exception);
            };
            return nextEventTask;
        }

        /// <summary>
        /// This is the method to call when this task is finished.
        /// </summary>
        /// <param name="result">The result from the operation.</param>
        /// <param name="exception">The exception from the operation, if any occucred.</param>
        public void OnFinished(T result,Exception exception)
        {
            ThreadPool.QueueUserWorkItem(o=>InnerFinish(result,exception,1));
        }

        private void InnerFinish(T result,Exception exception,int iteration)
        {
            var handler = OnFinish;
            if (handler==null)
            {
                if (iteration>10)
                {
                    //Write some debug information about this.
                    Debug.WriteLine("An event handler must be attached within a reasonable amount of time.");
                    return;
                }
                InnerFinish(result,exception,++iteration);

                //Wait some time to give the consumer the opportunity to attach an event handler for OnFinish.
                Thread.SpinWait(100000);
                return;
            }

            handler(this,
                    new CustomResultArgs<T>
                        {
                            ResultWrapper =
                                new ResultWrapper<T> {Result = result, Exception = exception, IsFaulted = exception != null}
                        });
        }
    }
}
