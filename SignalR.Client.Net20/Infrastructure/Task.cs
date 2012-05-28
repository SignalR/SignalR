using System;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Serialization;

namespace SignalR.Client.Net20.Infrastructure
{
	public class Task : Task<object>
	{
	}

	public class Task<T>
	{
		public event EventHandler<CustomResultArgs<T>> OnFinish;

		public Task<TFollowing> FollowedBy<TFollowing>(Func<T,TFollowing> nextAction)
		{
			var nextEventTask = new Task<TFollowing>();
			OnFinish += (sender, e) => nextEventTask.OnFinished(nextAction(e.ResultWrapper.Result),e.ResultWrapper.Exception);
			return nextEventTask;
		}

		public Task FollowedBy(Action<T> nextAction)
		{
			var nextEventTask = new Task();
			OnFinish += (sender, e) =>
			            	{
			            		nextAction(e.ResultWrapper.Result);
			            		nextEventTask.OnFinished(null, e.ResultWrapper.Exception);
			            	};
			return nextEventTask;
		}

		public Task FollowedByWithResult(Action<ResultWrapper<T>> nextAction)
		{
			var nextEventTask = new Task();
			OnFinish += (sender, e) =>
			{
				nextAction(e.ResultWrapper);
				nextEventTask.OnFinished(null, e.ResultWrapper.Exception);
			};
			return nextEventTask;
		}

		public void OnFinished(T result,Exception exception)
		{
			InnerFinish(result,exception,1);
		}

		private void InnerFinish(T result,Exception exception,int iteration)
		{
			var handler = OnFinish;
			if (handler==null)
			{
				if (iteration>10)
				{
					Debug.WriteLine("An event handler must be attached within a reasonable amount of time.");
					return;
				}
				InnerFinish(result,exception,++iteration);
				Thread.SpinWait(10000);
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

	public class CustomResultArgs<T> : EventArgs
	{
		public ResultWrapper<T> ResultWrapper { get; set; }
	}

	public class ResultWrapper<T>
	{
		public T Result { get; set; }
		public bool IsFaulted { get; set; }
		public Exception Exception { get; set; }

		public bool IsCanceled { get; set; }
	}

	public static class TaskAsyncHelper
	{
		public static Task Delay(TimeSpan timeSpan)
		{
			var newEvent = new Task();
			Thread.Sleep(timeSpan);
			newEvent.OnFinished(null, null);
			return newEvent;
		}

		public static Task Empty
		{
			get
			{
				var task = new Task();
				task.OnFinished(null,null);
				return task;
			}
		}
	}
}
