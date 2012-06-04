using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalR.Client
{
	public interface IObserver<T>
	{
		void OnCompleted();
		void OnError(Exception exception);
		void OnNext(T value);
	}
}
