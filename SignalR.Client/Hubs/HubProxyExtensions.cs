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
            if (value is JObject && typeof(T) != typeof(JObject))
            {
                return JsonConvert.DeserializeObject<T>(((JObject)value).ToString());
            }
            return (T)value;
        }

        public static void On<T>(this IHubProxy proxy, string eventName, Action<T> subscription)
        {
            proxy.Subscribe(eventName, args =>
            {
                subscription(Convert<T>(args[0]));
            });
        }

        public static void On<T1, T2>(this IHubProxy proxy, string eventName, Action<T1, T2> subscription)
        {
            proxy.Subscribe(eventName, args =>
            {
                subscription(Convert<T1>(args[0]),
                             Convert<T2>(args[1]));
            });
        }

        public static void On<T1, T2, T3>(this IHubProxy proxy, string eventName, Action<T1, T2, T3> subscription)
        {
            proxy.Subscribe(eventName, args =>
            {
                subscription(Convert<T1>(args[0]),
                             Convert<T2>(args[1]),
                             Convert<T3>(args[2]));
            });
        }

        public static void On<T1, T2, T3, T4>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4> subscription)
        {
            proxy.Subscribe(eventName, args =>
            {
                subscription(Convert<T1>(args[0]),
                             Convert<T2>(args[1]),
                             Convert<T3>(args[2]),
                             Convert<T4>(args[3]));
            });
        }

#if !WINDOWS_PHONE
        public static void On<T1, T2, T3, T4, T5>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5> subscription)
        {
            proxy.Subscribe(eventName, args =>
            {
                subscription(Convert<T1>(args[0]),
                             Convert<T2>(args[1]),
                             Convert<T3>(args[2]),
                             Convert<T4>(args[3]),
                             Convert<T5>(args[4]));
            });
        }

        public static void On<T1, T2, T3, T4, T5, T6>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5, T6> subscription)
        {
            proxy.Subscribe(eventName, args =>
            {
                subscription(Convert<T1>(args[0]),
                             Convert<T2>(args[1]),
                             Convert<T3>(args[2]),
                             Convert<T4>(args[3]),
                             Convert<T5>(args[4]),
                             Convert<T6>(args[5]));
            });
        }

        public static void On<T1, T2, T3, T4, T5, T6, T7>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4, T5, T6, T7> subscription)
        {
            proxy.Subscribe(eventName, args =>
            {
                subscription(Convert<T1>(args[0]),
                             Convert<T2>(args[1]),
                             Convert<T3>(args[2]),
                             Convert<T4>(args[3]),
                             Convert<T5>(args[4]),
                             Convert<T6>(args[5]),
                             Convert<T7>(args[6]));
            });
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
