using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal class TransportInitializationHandler
    {
        private ThreadSafeInvoker _initializationInvoker;
        private IConnection _connection;
        private IClientTransport _transport;
        private TaskCompletionSource<object> _initializationTask;

        public TransportInitializationHandler(IConnection connection, IClientTransport transport)
        {
            _transport = transport;
            _initializationTask = new TaskCompletionSource<object>();
            _initializationInvoker = new ThreadSafeInvoker();
            _connection = connection;

            TaskAsyncHelper.Delay(connection.TransportConnectTimeout.Value).Then(() =>
            {
                OnFailure(new TimeoutException(Resources.Error_TransportTimedOutTryingToConnect));
            });
        }

        public Task Task
        {
            get
            {
                return _initializationTask.Task;
            }
        }

        public void OnSuccess()
        {
            _initializationInvoker.Invoke(() =>
            {
                _initializationTask.SetResult(null);
            });
        }

        public void OnFailure(Exception ex)
        {
            _initializationInvoker.Invoke(() =>
            {
                _transport.Abort(_connection, TimeSpan.FromSeconds(1));
                _initializationTask.SetException(ex);
            });
        }
    }
}
