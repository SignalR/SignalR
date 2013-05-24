// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
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
        public static T GetValue<T>(this IHubProxy proxy, string name)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            return Convert<T>(proxy[name], proxy.JsonSerializer);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
        public static IDisposable On(this IHubProxy proxy, string eventName, Action onData)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData();
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
        public static IDisposable On<T>(this IHubProxy proxy, string eventName, Action<T> onData)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData(Convert<T>(args[0], proxy.JsonSerializer));
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
        public static IDisposable On<T1, T2>(this IHubProxy proxy, string eventName, Action<T1, T2> onData)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData(Convert<T1>(args[0], proxy.JsonSerializer),
                       Convert<T2>(args[1], proxy.JsonSerializer));
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
        public static IDisposable On<T1, T2, T3>(this IHubProxy proxy, string eventName, Action<T1, T2, T3> onData)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData(Convert<T1>(args[0], proxy.JsonSerializer),
                       Convert<T2>(args[1], proxy.JsonSerializer),
                       Convert<T3>(args[2], proxy.JsonSerializer));
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
        }

        /// <summary>
        /// Registers for an event with the specified name and callback
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/>.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="onData">The callback</param>
        /// <returns>An <see cref="IDisposable"/> that represents this subscription.</returns>
        public static IDisposable On<T1, T2, T3, T4>(this IHubProxy proxy, string eventName, Action<T1, T2, T3, T4> onData)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData(Convert<T1>(args[0], proxy.JsonSerializer),
                       Convert<T2>(args[1], proxy.JsonSerializer),
                       Convert<T3>(args[2], proxy.JsonSerializer),
                       Convert<T4>(args[3], proxy.JsonSerializer));
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
        }

#if !PORTABLE && !NET35 && !__ANDROID__ && !IOS
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
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData(Convert<T1>(args[0], proxy.JsonSerializer),
                       Convert<T2>(args[1], proxy.JsonSerializer),
                       Convert<T3>(args[2], proxy.JsonSerializer),
                       Convert<T4>(args[3], proxy.JsonSerializer),
                       Convert<T5>(args[4], proxy.JsonSerializer));
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
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
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData(Convert<T1>(args[0], proxy.JsonSerializer),
                       Convert<T2>(args[1], proxy.JsonSerializer),
                       Convert<T3>(args[2], proxy.JsonSerializer),
                       Convert<T4>(args[3], proxy.JsonSerializer),
                       Convert<T5>(args[4], proxy.JsonSerializer),
                       Convert<T6>(args[5], proxy.JsonSerializer));
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
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
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            if (onData == null)
            {
                throw new ArgumentNullException("onData");
            }

            Subscription subscription = proxy.Subscribe(eventName);

            Action<IList<JToken>> handler = args =>
            {
                onData(Convert<T1>(args[0], proxy.JsonSerializer),
                       Convert<T2>(args[1], proxy.JsonSerializer),
                       Convert<T3>(args[2], proxy.JsonSerializer),
                       Convert<T4>(args[3], proxy.JsonSerializer),
                       Convert<T5>(args[4], proxy.JsonSerializer),
                       Convert<T6>(args[5], proxy.JsonSerializer),
                       Convert<T7>(args[6], proxy.JsonSerializer));
            };

            subscription.Received += handler;

            return new DisposableAction(() => subscription.Received -= handler);
        }

        /// <summary>
        /// Registers a <see cref="IHubProxy"/> event has an <see cref="T:IObservable{T}"/>.
        /// </summary>
        /// <param name="proxy">The <see cref="IHubProxy"/></param>
        /// <param name="eventName">The name of the event.</param>
        /// <returns>An <see cref="T:IObservable{object[]}"/>.</returns>
        public static IObservable<IList<JToken>> Observe(this IHubProxy proxy, string eventName)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException("proxy");
            }

            if (String.IsNullOrEmpty(eventName))
            {
                throw new ArgumentNullException("eventName");
            }

            return new Hubservable(proxy, eventName);
        }
#endif

        private static T Convert<T>(JToken obj, JsonSerializer serializer)
        {
            if (obj == null)
            {
                return default(T);
            }

            return obj.ToObject<T>(serializer);
        }
    }
}
