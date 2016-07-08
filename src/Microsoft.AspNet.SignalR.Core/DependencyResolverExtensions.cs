// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
{
    public static class DependencyResolverExtensions
    {
        public static T Resolve<T>(this IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            return (T)resolver.GetService(typeof(T));
        }

        public static object Resolve(this IDependencyResolver resolver, Type type)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return resolver.GetService(type);
        }

        public static IEnumerable<T> ResolveAll<T>(this IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            return resolver.GetServices(typeof(T)).Cast<T>();
        }

        public static IEnumerable<object> ResolveAll(this IDependencyResolver resolver, Type type)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return resolver.GetServices(type);
        }
    }
}
