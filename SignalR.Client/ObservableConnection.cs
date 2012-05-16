using System;
using SignalR.Client.Infrastructure;
#if NET20
using SignalR.Client.Net20.Infrastructure;
using Newtonsoft.Json.Serialization;
#endif

namespace SignalR.Client
{
    public class ObservableConnection<T> : IObservable<T>
    {
        private readonly IConnection _connection;
        private readonly Func<string, T> _convert;

        public ObservableConnection(IConnection connection, Func<string, T> convert)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (convert == null)
            {
                throw new ArgumentNullException("convert");
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
