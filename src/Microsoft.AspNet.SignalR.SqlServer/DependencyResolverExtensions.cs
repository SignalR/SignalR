// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.SqlServer;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use SqlServer as the backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseSqlServer(this IDependencyResolver resolver, string connectionString)
        {
            return UseSqlServer(resolver, connectionString, tableCount: 1);
        }

        /// <summary>
        /// Use SqlServer as the backplane for SignalR.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <param name="tableCount">The number of tables to use as "message tables". Using more tables reduces lock contention and can increase throughput.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseSqlServer(this IDependencyResolver resolver, string connectionString, int tableCount)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            var bus = new SqlMessageBus(connectionString, tableCount, resolver);
            resolver.Register(typeof(IMessageBus), () => bus);

            return resolver;
        }
    }
}
