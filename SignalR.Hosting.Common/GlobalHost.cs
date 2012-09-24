﻿using System;
using SignalR.Hubs;

namespace SignalR
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
