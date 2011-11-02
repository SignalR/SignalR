using System;
using Newtonsoft.Json;

namespace SignalR.Client
{
    public static class ConnectionExtensions
    {
        public static IObservable<string> AsObservable(this IConnection connection)
        {
            return connection.AsObservable(value => value);
        }

        public static IObservable<T> AsObservable<T>(this IConnection connection)
        {
            return connection.AsObservable(value => JsonConvert.DeserializeObject<T>(value));
        }

        public static IObservable<T> AsObservable<T>(this IConnection connection, Func<string, T> selector)
        {
            return new ObservableConnection<T>(connection, selector);
        }
    }
}
