using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting.Common;
using SignalR.Hosting.Self.Infrastructure;

namespace SignalR.Hosting.Self
{
    public class Server : RoutingHost
    {
        private readonly string _url;
        private readonly HttpListener _listener;

        private Timer _heartBeat;
        private ConcurrentDictionary<HttpListenerResponseWrapper, bool> _aliveConnections = new ConcurrentDictionary<HttpListenerResponseWrapper, bool>();
        private int _checkingConnections;

        public Action<HostContext> OnProcessRequest { get; set; }

        public Server(string url)
            : this(url, Global.DependencyResolver)
        {

        }

        public Server(string url, IDependencyResolver resolver)
            : base(resolver)
        {
            _url = url;
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

            if (_heartBeat != null)
            {
                _heartBeat.Dispose();
                _heartBeat = null;
            }
        }

        private void ReceiveLoop()
        {
            _listener.BeginGetContext(ar =>
            {
                HttpListenerContext context;
                try
                {
                    context = _listener.EndGetContext(ar);

                    // Start the timer the checks for connection activity
                    if (_heartBeat == null)
                    {
                        var interval = TimeSpan.FromTicks(Configuration.HeartBeatInterval.Ticks / 2);
                        _heartBeat = new Timer(_ => CheckConnections(),
                                               null,
                                               interval,
                                               interval);
                    }
                }
                catch (Exception)
                {
                    return;
                }

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

                string path = ResolvePath(context.Request.Url);

                if (TryGetConnection(path, out connection))
                {
                    var request = new HttpListenerRequestWrapper(context.Request);
                    var response = new HttpListenerResponseWrapper(context.Response);
                    var hostContext = new HostContext(request, response, context.User);

                    if (OnProcessRequest != null)
                    {
                        OnProcessRequest(hostContext);
                    }

                    // Add this response to the list of live connections
                    _aliveConnections.TryAdd(response, true);
#if DEBUG
                    hostContext.Items[HostConstants.DebugMode] = true;
#endif
                    hostContext.Items["System.Net.HttpListenerContext"] = context;

                    // Initialize the connection
                    connection.Initialize(DependencyResolver);

                    return connection.ProcessRequestAsync(hostContext);
                }

                return context.Response.NotFound();
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
        }

        public string ResolvePath(Uri url)
        {
            string baseUrl = url.GetComponents(UriComponents.Scheme | UriComponents.HostAndPort | UriComponents.Path, UriFormat.SafeUnescaped);

            if (!baseUrl.StartsWith(_url, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unable to resolve path");
            }

            string path = baseUrl.Substring(_url.Length);
            if (!path.StartsWith("/"))
            {
                return "/" + path;
            }

            return path;
        }

        /// <summary>
        /// Checks to see if any of the active connections are still alive by writing a byte to the output
        /// stream and checking for an exception.
        /// </summary>
        private void CheckConnections()
        {
            if (Interlocked.Exchange(ref _checkingConnections, 1) == 1)
            {
                return;
            }

            if (_aliveConnections.Count > 0)
            {
                var dead = new List<HttpListenerResponseWrapper>();
                foreach (var c in _aliveConnections.Keys)
                {
                    if (!c.Ping())
                    {
                        dead.Add(c);
                    }
                }

                foreach (var c in dead)
                {
                    bool ignore;
                    _aliveConnections.TryRemove(c, out ignore);
                }
            }

            _checkingConnections = 0;
        }
    }
}
