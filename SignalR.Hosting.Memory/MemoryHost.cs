using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Infrastructure;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR.Hosting.Memory
{
    public class MemoryHost : IHttpClient
    {
        private readonly Dictionary<string, Type> _connectionMapping = new Dictionary<string, Type>();
        private bool _hubsEnabled;

        public IDependencyResolver DependencyResolver { get; private set; }

        public MemoryHost()
            : this(new DefaultDependencyResolver())
        {

        }

        public MemoryHost(IDependencyResolver resolver)
        {
            DependencyResolver = resolver;
        }

        public void MapConnection<T>(string path) where T : PersistentConnection
        {
            if (!_connectionMapping.ContainsKey(path))
            {
                _connectionMapping.Add(path, typeof(T));
            }
        }

        public void EnableHubs()
        {
            _hubsEnabled = true;
        }

        public bool TryGetConnection(string path, out PersistentConnection connection)
        {
            connection = null;

            if (_hubsEnabled && path.StartsWith("/signalr", StringComparison.OrdinalIgnoreCase))
            {
                connection = new HubDispatcher("/signalr");
                return true;
            }

            return TryGetMappedConnection(path, out connection);
        }

        private bool TryGetMappedConnection(string path, out PersistentConnection connection)
        {
            connection = null;

            foreach (var pair in _connectionMapping)
            {
                // If the url matches then create the connection type
                if (path.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    var factory = new PersistentConnectionFactory(DependencyResolver);
                    connection = factory.CreateInstance(pair.Value);
                    return true;
                }
            }

            return false;
        }

        Task<IHttpResponse> IHttpClient.GetAsync(string url, Action<IHttpRequest> prepareRequest)
        {
            return ProcessRequest(url, prepareRequest, postData: null);
        }

        Task<IHttpResponse> IHttpClient.PostAsync(string url, Action<IHttpRequest> prepareRequest, Dictionary<string, string> postData)
        {
            return ProcessRequest(url, prepareRequest, postData);
        }

        private Task<IHttpResponse> ProcessRequest(string url, Action<IHttpRequest> prepareRequest, Dictionary<string, string> postData)
        {
            var uri = new Uri(url);
            PersistentConnection connection;
            if (TryGetConnection(uri.LocalPath, out connection))
            {
                var cts = new CancellationTokenSource();
                var request = new Request(uri, cts, postData);
                prepareRequest(request);
                var response = new Response(cts.Token);
                var hostContext = new HostContext(request, response, null);

                // Initialize the connection
                connection.Initialize(DependencyResolver);

                var tcs = new TaskCompletionSource<IHttpResponse>();
                connection.ProcessRequestAsync(hostContext).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        tcs.SetException(task.Exception);
                    }
                    else if (task.IsCanceled)
                    {
                        tcs.SetCanceled();
                    }
                    else
                    {
                        tcs.SetResult(response);
                    }
                });

                return tcs.Task;
            }

            return TaskAsyncHelper.FromError<IHttpResponse>(new InvalidOperationException("Not a valid end point"));
        }
    }
}
