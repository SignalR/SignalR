using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.Client.Hubs
{
    public static class HubProxyExtensions
    {
        public static T GetValue<T>(this IHubProxy proxy, string name)
        {
            object value = proxy[name];
            return Convert<T>(value);
        }

        public static Subscription On(this IHubProxy proxy, string eventName, Action onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData();
            };

            return subscription;
        }

        public static Subscription On<T>(this IHubProxy proxy, string eventName, Action<T> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData(Convert<T>(args[0]));
            };

            return subscription;
        }

        public static Subscription On<T1, T2>(this IHubProxy proxy, string eventName, Action<T1, T2> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]));
            };

            return subscription;
        }

        public static Subscription On<T1, T2, T3>(this IHubProxy proxy, string eventName, Action<T1, T2, T3> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]));
            };

            return subscription;
        }

        public static Subscription On<T1, T2, T3, T4>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]));
            };

            return subscription;
        }

#if !WINDOWS_PHONE
        public static Subscription On<T1, T2, T3, T4, T5>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]),
                       Convert<T5>(args[4]));
            };

            return subscription;
        }

        public static Subscription On<T1, T2, T3, T4, T5, T6>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5, T6> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]),
                       Convert<T5>(args[4]),
                       Convert<T6>(args[5]));
            };

            return subscription;
        }

        public static Subscription On<T1, T2, T3, T4, T5, T6, T7>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5, T6, T7> onData)
        {
            Subscription subscription = proxy.Subscribe(eventName);
            subscription.Data += args =>
            {
                onData(Convert<T1>(args[0]),
                       Convert<T2>(args[1]),
                       Convert<T3>(args[2]),
                       Convert<T4>(args[3]),
                       Convert<T5>(args[4]),
                       Convert<T6>(args[5]),
                       Convert<T7>(args[6]));
            };

            return subscription;
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

            if (typeof(T) == typeof(string) && obj is string)
            {
                return (T)obj;
            }

            return JsonConvert.DeserializeObject<T>(obj.ToString());
        }
    }
}
