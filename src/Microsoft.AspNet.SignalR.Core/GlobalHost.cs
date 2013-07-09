// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// Provides access to default host information.
    /// </summary>
    public static class GlobalHost
    {
        private static readonly Lazy<IDependencyResolver> _defaultResolver = new Lazy<IDependencyResolver>(() => new DefaultDependencyResolver());
        private static IDependencyResolver _resolver;

        /// <summary>
        /// Gets or sets the the default <see cref="IDependencyResolver"/>
        /// </summary>
        public static IDependencyResolver DependencyResolver
        {
            get
            {
                return _resolver ?? _defaultResolver.Value;
            }
            set
            {
                _resolver = value;
            }
        }

        /// <summary>
        /// Gets the default <see cref="IConfigurationManager"/>
        /// </summary>
        public static IConfigurationManager Configuration
        {
            get
            {
                return DependencyResolver.Resolve<IConfigurationManager>();
            }
        }

        /// <summary>
        /// Gets the default <see cref="IConnectionManager"/>
        /// </summary>
        public static IConnectionManager ConnectionManager
        {
            get
            {
                return DependencyResolver.Resolve<IConnectionManager>();
            }
        }

        /// <summary>
        /// Gets the default <see cref="ITraceManager"/>
        /// </summary>
        public static ITraceManager TraceManager
        {
            get
            {
                return DependencyResolver.Resolve<ITraceManager>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static IHubPipeline HubPipeline
        {
            get
            {
                return DependencyResolver.Resolve<IHubPipeline>();
            }
        }
    }
}
