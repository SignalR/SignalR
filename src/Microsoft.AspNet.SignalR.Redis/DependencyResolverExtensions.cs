// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Redis;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, string server, int port, string password, IEnumerable<string> eventKeys)
        {
            return UseRedis(resolver, server, port, password, db: 0, eventKeys: eventKeys);
        }

        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, string server, int port, string password, int db, IEnumerable<string> eventKeys)
        {
            var bus = new Lazy<RedisMessageBus>(() => new RedisMessageBus(server, port, password, db, eventKeys, resolver));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
