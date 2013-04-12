// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Redis;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use the Redis backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="server">The redis server.</param>
        /// <param name="port">The redis port.</param>
        /// <param name="password">The redis server password.</param>
        /// <param name="eventKeys">The event keys.</param>
        /// <returns>The dependency resolver</returns>
        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, string server, int port, string password, IList<string> eventKeys)
        {
            return UseRedis(resolver, server, port, password, db: 0, eventKeys: eventKeys);
        }

        /// <summary>
        /// Use the Redis backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="server">The redis server.</param>
        /// <param name="port">The redis port.</param>
        /// <param name="password">The redis server password.</param>
        /// <param name="db">The database to use</param>
        /// <param name="eventKeys">The event keys.</param> 
        /// <returns></returns>
        public static IDependencyResolver UseRedis(this IDependencyResolver resolver, string server, int port, string password, int db, IList<string> eventKeys)
        {
            var bus = new Lazy<RedisMessageBus>(() => new RedisMessageBus(server, port, password, db, eventKeys, resolver));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
