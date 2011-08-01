using System;

namespace SignalR.Client {
    public class ObservableConnection : IObservable<string> {
        private readonly Connection _connection;
        public ObservableConnection(Connection connection) {
            if (connection == null) {
                throw new ArgumentNullException("connection");
            }

            _connection = connection;
        }

        public IDisposable Subscribe(IObserver<string> observer) {
            return new DisposableConnection(_connection, observer);
        }

        private class DisposableConnection : IDisposable {
            private readonly Connection _connection;
            private readonly IObserver<string> _observer;

            public DisposableConnection(Connection connection, IObserver<string> observer) {
                _connection = connection;
                _observer = observer;

                _connection.Received += OnReceived;
                _connection.Closed += OnClosed;
            }

            private void OnReceived(string data) {
                _observer.OnNext(data);
            }

            private void OnClosed() {
                _observer.OnCompleted();
            }

            public void Dispose() {
                _connection.Closed -= OnClosed;
                _connection.Received -= OnReceived;
            }
        }
    }
}
