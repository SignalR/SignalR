using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR.Hubs;
using SignalR.Server.Util;

namespace SignalR.Server.Handlers
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class HubDispatcherHandler
    {
        readonly AppFunc _app;
        readonly string _path;
        Func<IDependencyResolver> _resolver;

        public HubDispatcherHandler(AppFunc app)
        {
            _app = app;
            _path = "";

            // defer access to GlobalHost property to end-user to change resolver before first call
            _resolver = DeferredGlobalHostResolver;
        }

        public HubDispatcherHandler(AppFunc app, IDependencyResolver resolver)
        {
            _app = app;
            _path = "";
            _resolver = () => resolver;
        }

        public HubDispatcherHandler(AppFunc app, string path)
        {
            _app = app;
            _path = path;

            // defer access to GlobalHost property to end-user to change resolver before first call
            _resolver = DeferredGlobalHostResolver;
        }

        public HubDispatcherHandler(AppFunc app, string path, IDependencyResolver resolver)
        {
            _app = app;
            _path = path;
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

            var pathBase = Get<string>(env, OwinConstants.RequestPathBase);
            var dispatcher = new HubDispatcher(pathBase + _path);

            var handler = new CallHandler(_resolver.Invoke(), dispatcher);
            return handler.Invoke(env);
        }
    }
}
