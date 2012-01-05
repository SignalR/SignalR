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
#if !WINDOWS_PHONE && !SILVERLIGHT
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
#endif
    }
}
