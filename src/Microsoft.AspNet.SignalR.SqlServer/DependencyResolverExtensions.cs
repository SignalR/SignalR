// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.SqlServer;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use SQL Server as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseSqlServer(this IDependencyResolver resolver, string connectionString)
        {
            var config = new SqlScaleoutConfiguration(connectionString);

            return UseSqlServer(resolver, config);
        }

        /// <summary>
        /// Use SQL Server as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="configuration">The SQL scale-out configuration options.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseSqlServer(this IDependencyResolver resolver, SqlScaleoutConfiguration configuration)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            var bus = new Lazy<SqlMessageBus>(() => new SqlMessageBus(resolver, configuration));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
