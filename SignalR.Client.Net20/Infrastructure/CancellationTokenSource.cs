using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SignalR.Client.Net20.Infrastructure
{
	public class CancellationTokenSource
	{
		public void Cancel()
		{
			throw new NotImplementedException();
		}

		public bool IsCancellationRequested
		{
			get { throw new NotImplementedException(); }
		}
	}

	public class TaskCompletionSource<T>
	{
		public void SetException(Exception exception)
		{
			throw new NotImplementedException();
		}

		public void SetResult(T result)
		{
			throw new NotImplementedException();
		}

		public Task<T> Task
		{
			get { throw new NotImplementedException(); }
		}

		public void SetCanceled()
		{
			throw new NotImplementedException();
		}

		public void TrySetException(Exception exception)
		{
			throw new NotImplementedException();
		}

		public void TrySetResult(T result)
		{
			throw new NotImplementedException();
		}
	}

	public class AsyncTaskStuff
	{
		public delegate R AsyncTask<R>();

		public static AsyncTask<R> BeginTask<R>(AsyncTask<R> function)
		{
			R retv = default(R);
			bool completed = false;

			object sync = new object();

			IAsyncResult asyncResult = function.BeginInvoke(
				iAsyncResult =>
					{
						lock (sync)
						{
							completed = true;
							retv = function.EndInvoke(iAsyncResult);
							Monitor.Pulse(sync);
						}
					}, null);

			return delegate
			       	{
			       		lock (sync)
			       		{
			       			if (!completed)
			       			{
			       				Monitor.Wait(sync);
			       			}
			       			return retv;
			       		}
			       	};
		}
	}
}
