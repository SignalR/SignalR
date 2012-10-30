// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.SqlServer;

namespace Microsoft.AspNet.SignalR
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
