// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
namespace Microsoft.AspNet.SignalR
{
    public class ConnectionConfiguration
    {
        // Resolver isn't set to GlobalHost.DependencyResolver in the ctor because it is lazily created.
        private IDependencyResolver _resolver;

        /// <summary>
        /// The dependency resolver to use for the hub connection.
        /// </summary>
        public IDependencyResolver Resolver
        {
            get { return _resolver ?? GlobalHost.DependencyResolver; }
            set { _resolver = value; }
        }

        /// <summary>
        /// Gets of sets a boolean that determines if JSONP is enabled.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "JSONP", Justification = "JSONP is a known technology")]
        public bool EnableJSONP
        {
            get;
            set;
        }
    }
}
