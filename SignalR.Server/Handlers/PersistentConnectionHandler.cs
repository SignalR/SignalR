using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin;
using SignalR.Server.Util;

namespace SignalR.Server.Handlers
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class PersistentConnectionHandler
    {
        readonly AppFunc _app;
        readonly string _path;
        readonly Type _connectionType;
        Func<IDependencyResolver> _resolver;

        public PersistentConnectionHandler(AppFunc app, string path, Type connectionType)
        {
            _app = app;
            _path = path;
            _connectionType = connectionType;
            _resolver = DeferredGlobalHostResolver;
        }

        public PersistentConnectionHandler(AppFunc app, string path, Type connectionType, IDependencyResolver resolver)
        {
            _app = app;
            _path = path;
            _connectionType = connectionType;
            _resolver = () => resolver;
        }

        IDependencyResolver DeferredGlobalHostResolver()
        {
            var resolver = GlobalHost.DependencyResolver;
            _resolver = () => resolver;
            return resolver;
        }

        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            return env.TryGetValue(key, out value) ? (T)value : default(T);
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var path = Get<string>(env, OwinConstants.RequestPath);
            if (path == null || !path.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
            {
                return _app.Invoke(env);
            }

            var resolver = _resolver.Invoke();
            var connectionFactory = new PersistentConnectionFactory(resolver);
            var connection = connectionFactory.CreateInstance(_connectionType);

            var handler = new CallHandler(resolver, connection);
            return handler.Invoke(env);
        }
    }
}
