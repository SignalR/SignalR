using System;
using SignalR.Client.Infrastructure;

namespace SignalR.Client
{
    public class ObservableConnection<T> : IObservable<T>
    {
        private readonly Connection _connection;
        private readonly Func<string, T> _convert;

        public ObservableConnection(Connection connection, Func<string, T> convert)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (convert == null)
            {
                throw new ArgumentNullException("converter");
            }

            _convert = convert;
            _connection = connection;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            Action<string> received = data =>
            {
                observer.OnNext(_convert(data));
            };

            Action closed = () =>
            {
                observer.OnCompleted();
            };

            Action<Exception> error = ex =>
            {
                observer.OnError(ex);
            };

            _connection.Received += received;
            _connection.Closed += closed;
            _connection.Error += error;

            return new DisposableAction(() =>
            {
                _connection.Received -= received;
                _connection.Closed -= closed;
                _connection.Error -= error;
            });
        }
    }
}
