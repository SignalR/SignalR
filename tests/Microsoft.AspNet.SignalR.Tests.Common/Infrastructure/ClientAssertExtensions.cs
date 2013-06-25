using System;
using Microsoft.AspNet.SignalR.Client;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public static class ClientAssertExtensions
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        public static void SendWithTimeout(this Client.Connection connection, object value)
        {
            SendWithTimeout(connection, value, _defaultTimeout);
        }

        public static void SendWithTimeout(this Client.Connection connection, object value, TimeSpan timeout)
        {
            var task = connection.Send(value);

            Assert.True(task.Wait(timeout), "Failed to get response from send");
        }

        public static void SendWithTimeout(this Client.Connection connection, string data)
        {
            SendWithTimeout(connection, data, _defaultTimeout);
        }

        public static void SendWithTimeout(this Client.Connection connection, string data, TimeSpan timeout)
        {
            var task = connection.Send(data);

            Assert.True(task.Wait(timeout), "Failed to get response from send");
        }

        public static void InvokeWithTimeout(this IHubProxy proxy, string method, params object[] args)
        {
            InvokeWithTimeout(proxy, _defaultTimeout, method, args);
        }

        public static void InvokeWithTimeout(this IHubProxy proxy, TimeSpan timeout, string method, params object[] args)
        {
            var task = proxy.Invoke(method, args);

            Assert.True(task.Wait(timeout), "Failed to get response from " + method);
        }

        public static T InvokeWithTimeout<T>(this IHubProxy proxy, string method, params object[] args)
        {
            return InvokeWithTimeout<T>(proxy, _defaultTimeout, method, args);
        }

        public static T InvokeWithTimeout<T>(this IHubProxy proxy, TimeSpan timeout, string method, params object[] args)
        {
            var task = proxy.Invoke<T>(method, args);

            Assert.True(task.Wait(timeout), "Failed to get response from " + method);

            return task.Result;
        }
    }
}
