using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using SignalR.Abstractions;
using SignalR.Infrastructure;
using SignalR.SelfHost.Infrastructure;
using SignalR.Transports;

namespace SignalR.SelfHost
{
    public class Server
    {
        private readonly HttpListener _listener;
        private readonly Dictionary<string, Type> _connectionMapping = new Dictionary<string, Type>();

        static Server()
        {
            TransportManager.InitializeDefaultTransports();
        }

        public Server(string url)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
        }

        public void Start()
        {
            _listener.Start();

            ReceiveLoop();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public Server MapConnection<T>(string path) where T : PersistentConnection
        {
            if (!_connectionMapping.ContainsKey(path))
            {
                _connectionMapping.Add(path, typeof(T));
            }

            return this;
        }

        private void ReceiveLoop()
        {
            _listener.BeginGetContext(ar =>
            {
                HttpListenerContext context = _listener.EndGetContext(ar);
                ReceiveLoop();

                // Process the request async
                ProcessRequestAsync(context).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Exception ex = task.Exception.GetBaseException();
                        context.Response.ServerError(ex).Catch();

                        Debug.WriteLine(ex.Message);
                    }

                    context.Response.CloseSafe();
                });

            }, null);
        }

        private Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                Debug.WriteLine("Incoming request to {0}.", context.Request.Url);

                PersistentConnection connection;
                if (TryGetConnection(context, out connection))
                {
                    var request = new HttpListenerRequestWrapper(context.Request);
                    var response = new HttpListenerResponseWrapper(context.Response);
                    var hostContext = new HostContext(request, response, context.User);

                    hostContext.Items["net.HttpListenerContext"] = context;

                    return connection.ProcessRequestAsync(hostContext);
                }

                return context.Response.NotFound();
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
        }

        private bool TryGetConnection(HttpListenerContext context, out PersistentConnection connection)
        {
            connection = null;

            foreach (var pair in _connectionMapping)
            {
                // If the url matches then create the connection type
                if (context.Request.RawUrl.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    var factory = DependencyResolver.Resolve<IPersistentConnectionFactory>();
                    connection = factory.CreateInstance(pair.Value);
                    return true;
                }
            }

            return false;
        }
    }
}
