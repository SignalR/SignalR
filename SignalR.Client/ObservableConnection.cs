using System;

namespace SignalR.Client {
    public class ObservableConnection<T> : IObservable<T> {
        private readonly Connection _connection;
        private readonly Func<string, T> _convert;
        public ObservableConnection(Connection connection, Func<string, T> convert) {
            if (connection == null) {
                throw new ArgumentNullException("connection");
            }

            if (convert == null) {
                throw new ArgumentNullException("converter");
            }

            _convert = convert;
            _connection = connection;
        }

        public IDisposable Subscribe(IObserver<T> observer) {
            return new DisposableConnection(_connection, _convert, observer);
        }

        private class DisposableConnection : IDisposable {
            private readonly Connection _connection;
            private readonly Func<string, T> _convert;
            private readonly IObserver<T> _observer;

            public DisposableConnection(Connection connection, Func<string, T> convert, IObserver<T> observer) {
                _connection = connection;
                _convert = convert;
                _observer = observer;

                _connection.Received += OnReceived;
                _connection.Closed += OnClosed;
                _connection.Error += OnError;
            }

            private void OnReceived(string data) {
                _observer.OnNext(_convert(data));
            }

            private void OnClosed() {
                _observer.OnCompleted();
            }

            private void OnError(Exception error) {
                _observer.OnError(error);
            }

            public void Dispose() {
                _connection.Closed -= OnClosed;
                _connection.Received -= OnReceived;
                _connection.Error -= OnError;
            }
        }
    }
}
