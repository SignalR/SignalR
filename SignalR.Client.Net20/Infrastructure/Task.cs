using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace SignalR.Client.Net20.Infrastructure
{
	public class Task
	{
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

		public static Task Delay(TimeSpan timeSpan)
		{
			throw new NotImplementedException();
		}
	}

	public class Task<T> : Task
	{
		public Task<T> Then(Action<T> action)
		{
			throw new NotImplementedException();
		}

		public Task<T2> Then<T2>(Func<T,T2> action)
		{
			throw new NotImplementedException();
		}

		public void ContinueWith(Action<Task<T>> action)
		{
			throw new NotImplementedException();
		}

		public T Result
		{
			get { throw new NotImplementedException(); }
		}

		public void ContinueWithNotComplete(TaskCompletionSource<object> taskCompletionSource)
		{
			throw new NotImplementedException();
		}
	}

	public enum TaskContinuationOptions
	{
		ExecuteSynchronously
	}
}
