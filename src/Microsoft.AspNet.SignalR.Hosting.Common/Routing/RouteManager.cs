// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Hosting.Common.Routing
{
    public class RouteManager
    {
        private readonly IDependencyResolver _resolver;
        private readonly Dictionary<string, Type> _connectionMapping = new Dictionary<string, Type>();
        private string _hubPath;

        public RouteManager(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public void MapConnection<T>(string path) where T : PersistentConnection
        {
            if (!_connectionMapping.ContainsKey(path))
            {
                _connectionMapping.Add(path, typeof(T));
            }
        }

        public void MapHubs(string path)
        {
            _hubPath = path;
        }

        public bool TryGetConnection(string path, out PersistentConnection connection)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            connection = null;

            if (!String.IsNullOrEmpty(_hubPath) &&
                path.StartsWith(_hubPath, StringComparison.OrdinalIgnoreCase))
            {
                connection = new HubDispatcher(_hubPath);
                return true;
            }

            foreach (var pair in _connectionMapping)
            {
                // If the url matches then create the connection type
                if (path.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    var factory = new PersistentConnectionFactory(_resolver);
                    connection = factory.CreateInstance(pair.Value);
                    return true;
                }
            }

            return false;
        }
    }
}
