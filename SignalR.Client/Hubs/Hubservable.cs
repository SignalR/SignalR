using System;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Hubs
{
    /// <summary>
    /// <see cref="IObservable{object[]}"/> implementation of a hub event.
    /// </summary>
    public class Hubservable : IObservable<object[]>
    {
        private readonly string _eventName;
        private readonly IHubProxy _proxy;

        public Hubservable(IHubProxy proxy, string eventName)
        {
            _proxy = proxy;
            _eventName = eventName;
        }

        public IDisposable Subscribe(IObserver<object[]> observer)
        {
            var subscription = _proxy.Subscribe(_eventName);
            subscription.Data += observer.OnNext;

            return new DisposableAction(() =>
            {
                subscription.Data -= observer.OnNext;
            });
        }
    }
}
