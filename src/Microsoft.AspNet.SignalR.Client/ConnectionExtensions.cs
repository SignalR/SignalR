// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
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

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "jsonWriter will not dispose the stringWriter")]
        public static string JsonSerializeObject(this IConnection connection, object value)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var sb = new StringBuilder(0x100);
            using (var stringWriter = new StringWriter(sb, CultureInfo.InvariantCulture))
            {
                using (var jsonWriter = new JsonTextWriter(stringWriter) { CloseOutput = false })
                {
                    jsonWriter.Formatting = connection.JsonSerializer.Formatting;
                    connection.JsonSerializer.Serialize(jsonWriter, value);
                }

                return stringWriter.ToString();
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "jsonTextReader will not dispose the stringReader")]
        public static T JsonDeserializeObject<T>(this IConnection connection, string jsonValue)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            using (var stringReader = new StringReader(jsonValue))
            {
                using (var jsonTextReader = new JsonTextReader(stringReader) { CloseInput = false })
                {
                    return (T)connection.JsonSerializer.Deserialize(jsonTextReader, typeof(T));
                }
            }
        }

        public static bool EnsureReconnecting(this IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (connection.ChangeState(ConnectionState.Connected, ConnectionState.Reconnecting))
            {
                connection.OnReconnecting();
            }

            return connection.State == ConnectionState.Reconnecting;
        }

#if !PORTABLE && !__ANDROID__ && !IOS
        public static IObservable<string> AsObservable(this Connection connection)
        {
            return connection.AsObservable(value => value);
        }

        public static IObservable<T> AsObservable<T>(this Connection connection)
        {
            return connection.AsObservable(value => connection.JsonDeserializeObject<T>(value));
        }

        public static IObservable<T> AsObservable<T>(this Connection connection, Func<string, T> selector)
        {
            return new ObservableConnection<T>(connection, selector);
        }
#endif
    }
}
