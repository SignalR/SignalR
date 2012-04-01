using System;
using Newtonsoft.Json;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Hubs
{
    public static class HubProxyExtensions
    {
        public static T GetValue<T>(this IHubProxy proxy, string name)
        {
            object value = proxy[name];
            return Convert<T>(value);
        }

        public static IDisposable On(this IHubProxy proxy, string eventName, Action onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData();
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        public static IDisposable On<T>(this IHubProxy proxy, string eventName, Action<T> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T>(args[0]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        public static IDisposable On<T1, T2>(this IHubProxy proxy, string eventName, Action<T1, T2> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        public static IDisposable On<T1, T2, T3>(this IHubProxy proxy, string eventName, Action<T1, T2, T3> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        public static IDisposable On<T1, T2, T3, T4>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

#if !WINDOWS_PHONE && !SILVERLIGHT
        public static IDisposable On(this IHubProxy proxy, string eventName, Action<dynamic> onData)
        {
            return On<dynamic>(proxy, eventName, onData);
        }

        public static IDisposable On<T1, T2, T3, T4, T5>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]),
                       Convert<T5>(args[4]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        public static IDisposable On<T1, T2, T3, T4, T5, T6>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5, T6> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]),
                       Convert<T5>(args[4]),
                       Convert<T6>(args[5]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        public static IDisposable On<T1, T2, T3, T4, T5, T6, T7>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5, T6, T7> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]),
                       Convert<T5>(args[4]),
                       Convert<T6>(args[5]),
                       Convert<T7>(args[6]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        public static IObservable<object[]> Observe(this IHubProxy proxy, string eventName)
        {
            return new Hubservable(proxy, eventName);
        }
#endif
        private static T Convert<T>(object obj)
        {
            if (obj == null)
            {
                return default(T);
            }

#if !NETFX_CORE
            if (typeof(T).IsAssignableFrom(obj.GetType()))
            {
                return (T)obj;
            }
#endif

            return JsonConvert.DeserializeObject<T>(obj.ToString());
        }
    }
}
