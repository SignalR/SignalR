using System.Threading.Tasks;

namespace SignalR.Client.Transports
{
    public class AutoTransport : IClientTransport
    {
        // Transport that's in use
        private IClientTransport _transport;

        // List of transports in fallback order
        private readonly IClientTransport[] _transports = new IClientTransport[] { new ServerSentEventsTransport(), new LongPollingTransport() };

        public Task Start(Connection connection, string data)
        {
            var tcs = new TaskCompletionSource<object>();

            // Resolve the transport
            ResolveTransport(connection, data, tcs, 0);

            return tcs.Task;
        }

        private void ResolveTransport(Connection connection, string data, TaskCompletionSource<object> tcs, int index)
        {
            // Pick the current transport
            IClientTransport transport = _transports[index];

            transport.Start(connection, data).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // If that transport fails to initialize then fallback
                    var next = index + 1;
                    if (next < _transports.Length)
                    {
                        // Try the next transport
                        ResolveTransport(connection, data, tcs, next);
                    }
                    else
                    {
                        // If there's nothing else to try then just fail
                        tcs.SetException(task.Exception);
                    }
                }
                else
                {
                    // Set the active transport
                    _transport = transport;

                    // Complete the process
                    tcs.SetResult(null);
                }
            });
        }

        public Task<T> Send<T>(Connection connection, string data)
        {
            return _transport.Send<T>(connection, data);
        }

        public void Stop(Connection connection)
        {
            _transport.Stop(connection);
        }
    }
}
