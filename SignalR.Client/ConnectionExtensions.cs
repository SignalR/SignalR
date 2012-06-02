using System;
using Newtonsoft.Json;

namespace SignalR.Client
{
    public static class ConnectionExtensions
    {
        public static T GetValue<T>(this IConnection connection, string key)
        {
            object value;
            if (connection.Items.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return default(T);
        }

        public static bool IsDisconnecting(this IConnection connection)
        {
            return connection.State == ConnectionState.Disconnecting ||
                   connection.State == ConnectionState.Disconnected;
        }

        public static bool IsActive(this IConnection connection)
        {
            return connection.State == ConnectionState.Connected ||
                   connection.State == ConnectionState.Connecting;
        }

#if !WINDOWS_PHONE && !SILVERLIGHT
        public static IObservable<string> AsObservable(this Connection connection)
        {
            return connection.AsObservable(value => value);
        }

        public static IObservable<T> AsObservable<T>(this Connection connection)
        {
            return connection.AsObservable(value => JsonConvert.DeserializeObject<T>(value));
        }

        public static IObservable<T> AsObservable<T>(this Connection connection, Func<string, T> selector)
        {
            return new ObservableConnection<T>(connection, selector);
        }
#endif
    }
}
