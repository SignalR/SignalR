using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting.Common;
using SignalR.Hosting.Self.Infrastructure;

namespace SignalR.Hosting.Self
{
    public unsafe class Server : RoutingHost
    {
        private readonly string _url;
        private readonly HttpListener _listener;
        private CriticalHandle _requestQueueHandle;

        public Action<HostContext> OnProcessRequest { get; set; }

        public Server(string url)
            : this(url, Global.DependencyResolver)
        {

        }

        public Server(string url, IDependencyResolver resolver)
            : base(resolver)
        {
            _url = url.Replace("*", @".*?");
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
        }

        public void Start()
        {
            _listener.Start();

            // HACK: Get the request queue handle so we can register for disconnect
            var requestQueueHandleField = typeof(HttpListener).GetField("m_RequestQueueHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            if (requestQueueHandleField != null)
            {
                _requestQueueHandle = (CriticalHandle)requestQueueHandleField.GetValue(_listener);
            }

            ReceiveLoop();
        }

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

                var cts = new CancellationTokenSource();

                // Get the connection id value
                var connectionIdField = typeof(HttpListenerRequest).GetField("m_ConnectionId", BindingFlags.Instance | BindingFlags.NonPublic);
                if (_requestQueueHandle != null && connectionIdField != null)
                {
                    ulong connectionId = (ulong)connectionIdField.GetValue(context.Request);
                    // Create a nativeOverlapped callback so we can register for disconnect callback
                    var overlapped = new Overlapped();
                    var nativeOverlapped = overlapped.UnsafePack((errorCode, numBytes, pOVERLAP) =>
                    {
                        // Free the overlapped
                        Overlapped.Free(pOVERLAP);

                        // Mark the client as disconnected
                        cts.Cancel();
                    },
                    null);

                    uint hr = NativeMethods.HttpWaitForDisconnect(_requestQueueHandle, connectionId, nativeOverlapped);

                    if (hr != NativeMethods.HttpErrors.ERROR_IO_PENDING &&
                        hr != NativeMethods.HttpErrors.NO_ERROR)
                    {
                        // We got an unknown result so throw
                        throw new InvalidOperationException("Unable to register disconnect callback");
                    }
                }
                else
                {
                    Debug.WriteLine("Unable to resolve requestQueue handle. Disconnect notifications will be ignored");
                }

                ReceiveLoop();

                // Process the request async
                ProcessRequestAsync(context, cts.Token).ContinueWith(task =>
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

        private Task ProcessRequestAsync(HttpListenerContext context, CancellationToken token)
        {
            try
            {
                Debug.WriteLine("Incoming request to {0}.", context.Request.Url);

                PersistentConnection connection;

                string path = ResolvePath(context.Request.Url);

                if (TryGetConnection(path, out connection))
                {
                    var request = new HttpListenerRequestWrapper(context.Request);
                    var response = new HttpListenerResponseWrapper(context.Response, token);
                    var hostContext = new HostContext(request, response, context.User);

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
