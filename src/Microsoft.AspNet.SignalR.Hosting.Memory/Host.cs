﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Hosting.Common
{
    public class Host
    {
        private readonly IDependencyResolver _resolver;
        
        public Host(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Gets the <see cref="IDependencyResolver"/> for this host.
        /// </summary>
        public IDependencyResolver DependencyResolver
        {
            get
            {
                return _resolver;
            }
        }

        /// <summary>
        /// Gets the <see cref="IConnectionManager"/> for this host.
        /// </summary>
        public IConnectionManager ConnectionManager
        {
            get
            {
                return DependencyResolver.Resolve<IConnectionManager>();
            }
        }

        /// <summary>
        /// Gets the <see cref="IConfigurationManager"/> for this host.
        /// </summary>
        public IConfigurationManager Configuration
        {
            get
            {
                return DependencyResolver.Resolve<IConfigurationManager>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IHubPipeline HubPipeline
        {
            get
            {
                return DependencyResolver.Resolve<IHubPipeline>();
            }
        }
    }
}
