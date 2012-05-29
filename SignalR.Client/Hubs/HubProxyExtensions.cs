using System;
using Newtonsoft.Json;
using SignalR.Client.Infrastructure;
#if NET20
using Newtonsoft.Json.Serialization;
#endif

namespace SignalR.Client.Hubs
{
    /// <summary>
    /// Extensions to the <see cref="IHubProxy"/>.
    /// </summary>
    public static class HubProxyExtensions
    {
        /// <summary>
        /// Gets the value of a state variable.
        /// </summary>
        /// <typeparam name="T">The type of the state variable</typeparam>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="name">The name of the state variable.</param>
        /// <returns>The value of the state variable.</returns>
#if NET20
        public static T GetValue<T>( IHubProxy proxy, string name)
#else
        public static T GetValue<T>(this IHubProxy proxy, string name)
#endif
        {
            object value = proxy[name];
            return Convert<T>(value);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
#if NET20
		public static IDisposable On( IHubProxy proxy, string eventName, Action onData)
#else
		public static IDisposable On(this IHubProxy proxy, string eventName, Action onData)
#endif
		{
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData();
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
#if NET20
        public static IDisposable On<T>( IHubProxy proxy, string eventName, Action<T> onData)
#else
        public static IDisposable On<T>(this IHubProxy proxy, string eventName, Action<T> onData)
#endif
        {
            Subscription subscription = proxy.Subscribe(eventName);

            Action<object[]> handler = args =>
            {
                onData(Convert<T>(args[0]));
            };

            subscription.Data += handler;

            return new DisposableAction(() => subscription.Data -= handler);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
#if NET20
        public static IDisposable On<T1, T2>( IHubProxy proxy, string eventName, Action<T1, T2> onData)
#else
        public static IDisposable On<T1, T2>(this IHubProxy proxy, string eventName, Action<T1, T2> onData)
#endif
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

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
#if NET20
        public static IDisposable On<T1, T2, T3>(IHubProxy proxy, string eventName, Action<T1, T2, T3> onData)
#else
        public static IDisposable On<T1, T2, T3>(this IHubProxy proxy, string eventName, Action<T1, T2, T3> onData)
#endif
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

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
#if NET20
        public static IDisposable On<T1, T2, T3, T4>( IHubProxy proxy, string eventName, Action<T1, T2, T3, T4> onData)
#else
        public static IDisposable On<T1, T2, T3, T4>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4> onData)
#endif
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

#if !WINDOWS_PHONE && !SILVERLIGHT && !NET20
        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
        public static IDisposable On(this IHubProxy proxy, string eventName, Action<dynamic> onData)
        {
            return On<dynamic>(proxy, eventName, onData);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
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

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
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

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
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

        /// <summary>
        /// Registers a <see cref="IHubProxy"/> event has an <see cref="IObservable{T}"/>.
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/></param>
        /// <param name="eventName">The name of the event.</param>
        /// <returns>An <see cref="T:System.IObservable{object[]}"/>.</returns>
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

            if (typeof(T).IsAssignableFrom(obj.GetType()))
            {
                return (T)obj;
            }

            return JsonConvert.DeserializeObject<T>(obj.ToString());
        }
    }
}
