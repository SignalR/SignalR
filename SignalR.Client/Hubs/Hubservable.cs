using System;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Hubs
{
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
            _proxy.Subscribe(_eventName, args =>
            {
                observer.OnNext(args);
            });

            return new DisposableAction(() =>
            {
                _proxy.Unsubscribe(_eventName);
            });
        }
    }
}
