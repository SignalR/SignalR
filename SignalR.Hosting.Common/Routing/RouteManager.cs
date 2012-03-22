using System;
using System.Collections.Generic;
using SignalR.Hubs;

namespace SignalR.Hosting.Common.Routing
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

        public bool TryGetConnection(Uri uri, out PersistentConnection connection)
        {
            return TryGetConnection(uri.LocalPath, out connection);
        }

        public bool TryGetConnection(string path, out PersistentConnection connection)
        {
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
