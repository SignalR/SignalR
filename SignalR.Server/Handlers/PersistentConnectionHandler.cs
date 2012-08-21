using System;
using System.Threading.Tasks;
using Owin;
using SignalR.Server.Utils;

namespace SignalR.Server.Handlers
{
    public class PersistentConnectionHandler
    {
        readonly AppDelegate _app;
        readonly string _path;
        readonly Type _connectionType;
        Func<IDependencyResolver> _resolver;

        public PersistentConnectionHandler(AppDelegate app, string path, Type connectionType)
        {
            _app = app;
            _path = path;
            _connectionType = connectionType;
            _resolver = DeferredGlobalHostResolver;
        }

        public PersistentConnectionHandler(AppDelegate app, string path, Type connectionType, IDependencyResolver resolver)
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

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            var path = call.Path();
            if (path == null || !path.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
            {
                return _app.Invoke(call);
            }

            var resolver = _resolver.Invoke();
            var connectionFactory = new PersistentConnectionFactory(resolver);
            var connection = connectionFactory.CreateInstance(_connectionType);

            var handler = new CallHandler(resolver, connection);
            return handler.Invoke(call);
        }
    }
}
