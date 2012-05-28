using System;
using Newtonsoft.Json.Serialization;

namespace SignalR.Client.Net20.Infrastructure
{
	public class Task
	{
		protected virtual void Execute()
		{
		}

		public void ContinueWith(Task task)
		{
			throw new NotImplementedException();
		}

		public void ContinueWith(TaskCompletionSource<object> task)
		{
			throw new NotImplementedException();
		}

		public void ContinueWith(Action<Task> action)
		{
			throw new NotImplementedException();
		}

		public Task Then(Action action)
		{
			throw new NotImplementedException();
		}

		public bool IsFaulted
		{
			get { return false; }
		}

		public Exception Exception
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsCanceled
		{
			get { throw new NotImplementedException(); }
		}

		public void ContinueWith(Action<Task> action, TaskContinuationOptions taskContinuationOptions)
		{
			throw new NotImplementedException();
		}

		internal static class Factory
		{
			public static Task<T> FromAsync<T>(Func<AsyncCallback,object,IAsyncResult> beginMethod, Func<IAsyncResult,T> endMethod, object state)
			{
				throw new NotImplementedException();
			}

			public static Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state)
			{
				throw new NotImplementedException();
			}
		}
	}

	public static class TaskAsyncHelper
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

		public static Task<T> FromResult<T>(T value)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetResult(value);
			return tcs.Task;
		}

		internal static Task FromError(Exception exception)
		{
			var tcs = new TaskCompletionSource<object>();
			tcs.SetException(exception);
			return tcs.Task;
		}

		internal static Task<T> FromError<T>(Exception exception)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(exception);
			return tcs.Task;
		}

		public static Task Delay(TimeSpan timeSpan)
		{
			throw new NotImplementedException();
		}
	}

	public class Task<T> : Task
	{
		private readonly AsyncTaskStuff.AsyncTask<T> _action;

		public Task(Func<T> action)
		{
			_action = new AsyncTaskStuff.AsyncTask<T>(action);
		}

		private Task(Action<T> action)
		{
			_action = new AsyncTaskStuff.AsyncTask<T>(()=>action);
		}

		private Task(Func<T, object> action)
		{
			_action = new AsyncTaskStuff.AsyncTask<T>(action);
		}

		protected override void Execute()
		{
			if (_action!=null)
			{
				//var task = AsyncTaskStuff.BeginTask(() => _action);
				//task();
			}
		}

		public Task<T> Then(Action<T> action)
		{
			AsyncTaskStuff.BeginTask(_action);
			return new Task<T>(action);
		}

		public Task<T2> Then<T2>(Func<T,T2> action)
		{
			AsyncTaskStuff.BeginTask(_action);
			return new Task<T>(action);
		}

		public void ContinueWith(Action<Task<T>> action)
		{
			throw new NotImplementedException();
		}

		public T Result
		{
			get { throw new NotImplementedException(); }
		}

		public void ContinueWithNotComplete(TaskCompletionSource<object> tcs)
		{
			ContinueWith(t =>
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
	}

	public enum TaskContinuationOptions
	{
		ExecuteSynchronously,
		NotOnRanToCompletion
	}
}
