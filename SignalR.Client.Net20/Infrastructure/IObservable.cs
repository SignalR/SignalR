using System;
using System.Collections.Generic;
using System.Text;

namespace SignalR.Client.Net20.Infrastructure
{
	interface IObservable<T>
	{
	}

	public interface IObserver<T>
	{
		void OnNext(T value);
		void OnCompleted();
		void OnError(Exception exception);
	}
}
