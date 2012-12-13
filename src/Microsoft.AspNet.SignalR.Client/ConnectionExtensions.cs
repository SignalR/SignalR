// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Client
{
    public static class ConnectionExtensions
    {
        public static T GetValue<T>(this IConnection connection, string key)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            lock (connection.Items)
            {
                object value;
                if (connection.Items.TryGetValue(key, out value))
                {
                    return (T)value;
                }
            }

            return default(T);
        }

        public static bool EnsureReconnecting(this IConnection connection)
        {
            if (connection.ChangeState(ConnectionState.Connected, ConnectionState.Reconnecting))
            {
                connection.OnReconnecting();
            }
            return connection.State == ConnectionState.Reconnecting;
        }

#if !WINDOWS_PHONE && !SILVERLIGHT && !NET35
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
