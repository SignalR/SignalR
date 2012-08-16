using System;
using System.Threading.Tasks;
using Owin;

namespace SignalR.Server
{
    public class PersistentConnectionHandler
    {
        readonly AppDelegate _app;
        readonly string _url;
        readonly Type _connectionType;
        readonly IDependencyResolver _resolver;

        public PersistentConnectionHandler(AppDelegate app, string url, Type connectionType, IDependencyResolver resolver)
        {
            _app = app;
            _url = url;
            _connectionType = connectionType;
            _resolver = resolver;
        }

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            var factory = new PersistentConnectionFactory(_resolver);
            PersistentConnection connection = factory.CreateInstance(_connectionType);

            var callContext = new CallContext(_resolver, connection);
            return callContext.Invoke(call);
        }
    }
}
