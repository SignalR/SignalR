using System;
using System.Collections.Generic;

namespace SignalR.SqlServer
{
    public static class DependencyResolverExtensions
    {
        public static IDependencyResolver UseSqlServer(this IDependencyResolver resolver, string connectionString)
        {
            return UseSqlServer(resolver, connectionString, 1);
        }

        public static IDependencyResolver UseSqlServer(this IDependencyResolver resolver, string connectionString, int tableCount)
        {
            var bus = new Lazy<SqlMessageBus>(() => new SqlMessageBus(connectionString, tableCount, resolver));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
