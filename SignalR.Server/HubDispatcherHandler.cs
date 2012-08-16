using System;
using System.Threading.Tasks;
using Owin;
using SignalR.Hubs;

namespace SignalR.Server
{
    public class HubDispatcherHandler
    {
        readonly AppDelegate _app;
        readonly string _url;
        Func<IDependencyResolver> _resolver;

        public HubDispatcherHandler(AppDelegate app)
        {
            _app = app;
            _url = "/signalr";

            // defer access to GlobalHost property to end-user to change resolver before first call
            _resolver = DeferredGlobalHostResolver;
        }

        public HubDispatcherHandler(AppDelegate app, IDependencyResolver resolver)
        {
            _app = app;
            _url = "/signalr";
            _resolver = () => resolver;
        }

        public HubDispatcherHandler(AppDelegate app, string url)
        {
            _app = app;
            _url = url;

            // defer access to GlobalHost property to end-user to change resolver before first call
            _resolver = DeferredGlobalHostResolver;
        }

        public HubDispatcherHandler(AppDelegate app, string url, IDependencyResolver resolver)
        {
            _app = app;
            _url = url;
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
            var dispatcher = new HubDispatcher(_url);
            var callContext = new CallContext(_resolver.Invoke(), dispatcher);
            return callContext.Invoke(call);
        }
    }
}
