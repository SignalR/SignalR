using System;
using System.Collections.Generic;
using SignalR.Infrastructure;

namespace SignalR.Hosting.Common.Routing
{
    public class RouteManager
    {
        private readonly IDependencyResolver _resolver;
        private readonly Dictionary<string, Type> _connectionMapping = new Dictionary<string, Type>();

        public RouteManager(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public void MapConnection<T>(string path) where T : PersistentConnection
        {
            if (!_connectionMapping.ContainsKey(path))
            {
                _connectionMapping.Add(path, typeof(T));
            }
        }

        public bool TryGetConnection(Uri uri, out PersistentConnection connection)
        {
            return TryGetConnection(uri.LocalPath, out connection);
        }

        public bool TryGetConnection(string path, out PersistentConnection connection)
        {
            connection = null;

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
