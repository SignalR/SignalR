using System;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Hubs {
    public class Hubservable : IObservable<object[]> {
        private readonly string _eventName;
        private readonly HubProxy _proxy;

        public Hubservable(HubProxy proxy, string eventName) {
            _proxy = proxy;
            _eventName = eventName;
        }

        public IDisposable Subscribe(IObserver<object[]> observer) {
            _proxy.MethodInvoked += info => {
                if (info.Method.Equals(_eventName, StringComparison.OrdinalIgnoreCase)) {
                    observer.OnNext(info.Args);
                }
            };

            return new DisposableAction(() => {
                _proxy.RemoveEvent(_eventName);
            });
        }
    }
}
