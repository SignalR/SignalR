using System.Threading.Tasks;
using Owin;

namespace SignalR.Server
{
    public class CallContext
    {
        readonly IDependencyResolver _resolver;
        readonly PersistentConnection _connection;

        public CallContext(IDependencyResolver resolver, PersistentConnection connection)
        {
            _resolver = resolver;
            _connection = connection;
        }

        public Task<ResultParameters> Invoke(CallParameters call)
        {
            var tcs = new TaskCompletionSource<ResultParameters>();

            var request = new ServerRequest(call);
            var response = new ServerResponse(call, tcs);
            var hostContext = new HostContext(request, response);

            object value;
            if (call.Environment.TryGetValue("owin.CallCompleted", out value))
            {
                var callDisposed = value as Task;
                if (callDisposed != null)
                {
                    callDisposed.Finally(response.OnCallCompleted);
                }
            }

            _connection.Initialize(_resolver);

            _connection
                .ProcessRequestAsync(hostContext)
                .Finally(response.OnCallCompleted);

            return tcs.Task;
        }
    }
}
