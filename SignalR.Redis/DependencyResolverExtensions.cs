using System;
using System.Collections.Generic;
using SignalR.Infrastructure;

namespace SignalR.Redis
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
