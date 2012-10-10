using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SignalR.Client.Http;

namespace SignalR.Client.Transports
{
    public class AutoTransport : IClientTransport
    {
        // Transport that's in use
        private IClientTransport _transport;

        private readonly IHttpClient _httpClient;

        // List of transports in fallback order
        private readonly IClientTransport[] _transports;

        private TimeSpan? _keepAliveToRegister;

        public AutoTransport(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _transports = new IClientTransport[] { new ServerSentEventsTransport(httpClient), new LongPollingTransport(httpClient) };
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return HttpBasedTransport.GetNegotiationResponse(_httpClient, connection);
        }

        public Task Start(IConnection connection, string data)
        {
            var tcs = new TaskCompletionSource<object>();

            // Resolve the transport
            ResolveTransport(connection, data, tcs, 0);

            return tcs.Task;
        }

        private void ResolveTransport(IConnection connection, string data, TaskCompletionSource<object> tcs, int index)
        {
            // Pick the current transport
            IClientTransport transport = _transports[index];

            transport.Start(connection, data).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // Make sure we observe the exception
                    var ex = task.Exception;
#if !WINDOWS_PHONE && !SILVERLIGHT && !NETFX_CORE
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
#endif
#if NET35
                    Debug.WriteLine(System.String.Format(System.Globalization.CultureInfo.InvariantCulture, "Auto: Failed to connect to using transport {0}", (object)transport.GetType().Name));
#else
                    Debug.WriteLine("Auto: Failed to connect to using transport {0}", (object)transport.GetType().Name);
#endif

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

                    if (_keepAliveToRegister.HasValue)
                    {
                        _transport.RegisterKeepAlive(_keepAliveToRegister.Value);
                    }

                    // Complete the process
                    tcs.SetResult(null);
                }

            });
        }

        public Task<T> Send<T>(IConnection connection, string data)
        {
            return _transport.Send<T>(connection, data);
        }

        public void Stop(IConnection connection)
        {
            Stop(connection, notifyServer: true);
        }

        public void Stop(IConnection connection, bool notifyServer)
        {
            if (_transport != null)
            {
                _transport.Stop(connection);
            }
        }

        public void RegisterKeepAlive(TimeSpan keepAlive)
        {
            _keepAliveToRegister = keepAlive;
        }

        public virtual bool SupportsKeepAlive
        {
            get
            {
                return _transport.SupportsKeepAlive;
            }
            set
            {
                _transport.SupportsKeepAlive = value;
            }
        }

        public void MonitorKeepAlive(IConnection connection)
        {
            _transport.MonitorKeepAlive(connection);
        }

        public void StopMonitoringKeepAlive()
        {
            _transport.StopMonitoringKeepAlive();
        }
    }
}
