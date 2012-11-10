// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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

        public static void InitializeHost(this IDependencyResolver resolver, string instanceName, CancellationToken hostShutdownToken)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            if (String.IsNullOrEmpty(instanceName))
            {
                throw new ArgumentNullException("instanceName");
            }

            // Initialize the performance counters
            resolver.InitializePerformanceCounters(instanceName, hostShutdownToken);

            // Dispose the dependency resolver on host shut down (cleanly)
            resolver.InitializeResolverDispose(hostShutdownToken);
        }

        private static void InitializePerformanceCounters(this IDependencyResolver resolver, string instanceName, CancellationToken hostShutdownToken)
        {
            var counters = resolver.Resolve<IPerformanceCounterManager>();
            if (counters != null)
            {
                counters.Initialize(instanceName, hostShutdownToken);
            }
        }

        private static void InitializeResolverDispose(this IDependencyResolver resolver, CancellationToken hostShutdownToken)
        {
            // TODO: Guard against multiple calls to this

            // When the host triggers the shutdown token, dispose the resolver
            hostShutdownToken.Register(state =>
            {
                ((IDependencyResolver)state).Dispose();
            },
            resolver,
            useSynchronizationContext: false);
        }
    }
}
