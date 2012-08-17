using System;
using System.Threading.Tasks;
using Owin;
using SignalR.Hubs;
using SignalR.Server.Utils;

namespace SignalR.Server
{
    public class HubDispatcherHandler
    {
        readonly AppDelegate _app;
        readonly string _path;
        Func<IDependencyResolver> _resolver;

        public HubDispatcherHandler(AppDelegate app)
        {
            _app = app;
            _path = "";

            // defer access to GlobalHost property to end-user to change resolver before first call
            _resolver = DeferredGlobalHostResolver;
        }

        public HubDispatcherHandler(AppDelegate app, IDependencyResolver resolver)
        {
            _app = app;
            _path = "";
            _resolver = () => resolver;
        }

        public HubDispatcherHandler(AppDelegate app, string path)
        {
            _app = app;
            _path = path;

            // defer access to GlobalHost property to end-user to change resolver before first call
            _resolver = DeferredGlobalHostResolver;
        }

        public HubDispatcherHandler(AppDelegate app, string path, IDependencyResolver resolver)
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


        public Task<ResultParameters> Invoke(CallParameters call)
        {
            var path = call.Path();
            if (path != null && path.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
            {
                var dispatcher = new HubDispatcher(call.PathBase() + _path);
                var callContext = new CallContext(_resolver.Invoke(), dispatcher);
                return callContext.Invoke(call);
            }
            return _app.Invoke(call);
        }
    }
}
