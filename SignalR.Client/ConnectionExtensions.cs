using System;

namespace SignalR.Client {
    public static class ConnectionExtensions {
        public static IObservable<string> AsObservable(this Connection connection) {
            return new ObservableConnection(connection);
        }
    }
}
