using System;
using Newtonsoft.Json;

namespace SignalR.Client {
    public static class ConnectionExtensions {
        public static IObservable<string> AsObservable(this Connection connection) {
            return new ObservableConnection<string>(connection, value => value);
        }

        public static IObservable<T> AsObservable<T>(this Connection connection) {
            return new ObservableConnection<T>(connection, value => JsonConvert.DeserializeObject<T>(value));
        }
    }
}
