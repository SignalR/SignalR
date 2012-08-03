using SignalR.Hosting.Common;
using SignalR.Hosting.Self.Infrastructure;
using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Hosting.Self
{
    public unsafe class Server : RoutingHost
    {
        private readonly string _url;
        private readonly HttpListener _listener;
        private CriticalHandle _requestQueueHandle;
        private DisconnectHandler _disconnectHandler;

        public Action<HostContext> OnProcessRequest { get; set; }

        /// <summary>
        /// Initializes new instance of <see cref="Server"/>.
        /// </summary>
        /// <param name="url">The url to host the server on.</param>
        public Server(string url)
            : this(url, GlobalHost.DependencyResolver)
        {
        }

        /// <summary>
        /// Initializes new instance of <see cref="Server"/>.
        /// </summary>
        /// <param name="url">The url to host the server on.</param>
        /// <param name="resolver">The dependency resolver for the server.</param>
        public Server(string url, IDependencyResolver resolver)
            : base(resolver)
        {
            _url = url.Replace("*", @".*?");
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _disconnectHandler = new DisconnectHandler(_listener);
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get { return _listener.AuthenticationSchemes; }
            set { _listener.AuthenticationSchemes = value; }
        }

        /// <summary>
        /// Starts the server connection.
        /// </summary>
        public void Start()
        {
            _listener.Start();

            _disconnectHandler.Initialize();

            ReceiveLoop();
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
        }

        private void ReceiveLoop()
        {
            _listener.BeginGetContext(ar =>
            {
                HttpListenerContext context;
                try
                {
                    context = _listener.EndGetContext(ar);
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
                Debug.WriteLine("Server: Incoming request to {0}.", context.Request.Url);

                PersistentConnection connection;

                string path = ResolvePath(context.Request.Url);

                if (TryGetConnection(path, out connection))
                {
                    // https://developer.mozilla.org/En/HTTP_Access_Control
                    string origin = context.Request.Headers["Origin"];
                    if (!String.IsNullOrEmpty(origin))
                    {
                        context.Response.AddHeader("Access-Control-Allow-Origin", origin);
                        context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
                    }

                    var request = new HttpListenerRequestWrapper(context);
                    var response = new HttpListenerResponseWrapper(context.Response, _disconnectHandler.GetDisconnectToken(context));
                    var hostContext = new HostContext(request, response);

#if NET45
                    hostContext.Items[HostConstants.SupportsWebSockets] = Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2;
#endif

                    if (OnProcessRequest != null)
                    {
                        OnProcessRequest(hostContext);
                    }

#if DEBUG
                    hostContext.Items[HostConstants.DebugMode] = true;
#endif
                    hostContext.Items["System.Net.HttpListenerContext"] = context;

                    // Initialize the connection
                    connection.Initialize(DependencyResolver);

                    return connection.ProcessRequestAsync(hostContext);
                }

                if (path.Equals("/clientaccesspolicy.xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    return context.Response.WriteAsync(Resources.ClientAccessPolicyXml);
                }

                return context.Response.NotFound();
            }
            catch (Exception ex)
            {
                return TaskAsyncHelper.FromError(ex);
            }
        }

        private string ResolvePath(Uri url)
        {
            string baseUrl = url.GetComponents(UriComponents.Scheme | UriComponents.HostAndPort | UriComponents.Path, UriFormat.SafeUnescaped);

            Match match = Regex.Match(baseUrl, "^" + _url);
            if (!match.Success)
            {
                throw new InvalidOperationException("Unable to resolve path");
            }

            string path = baseUrl.Substring(match.Value.Length);
            if (!path.StartsWith("/"))
            {
                return "/" + path;
            }

            return path;
        }


    }
}
