// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    /// <summary>
    /// <see cref="T:System.IObservable{object[]}"/> implementation of a hub event.
    /// </summary>
    public class Hubservable : IObservable<JToken[]>
    {
        private readonly string _eventName;
        private readonly IHubProxy _proxy;

        public Hubservable(IHubProxy proxy, string eventName)
        {
            _proxy = proxy;
            _eventName = eventName;
        }

        public IDisposable Subscribe(IObserver<JToken[]> observer)
        {
            var subscription = _proxy.Subscribe(_eventName);
            subscription.Data += observer.OnNext;

            return new DisposableAction(() =>
            {
                subscription.Data -= observer.OnNext;
            });
        }
    }
}
