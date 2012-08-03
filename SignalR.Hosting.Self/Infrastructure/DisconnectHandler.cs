using SignalR.Hosting.Self.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;


namespace SignalR.Hosting.Self
{
    public unsafe class DisconnectHandler
    {
        private readonly ConcurrentDictionary<ulong, Lazy<CancellationTokenSource>> _connectionCancellationTokens = new ConcurrentDictionary<ulong, Lazy<CancellationTokenSource>>();
        private readonly HttpListener _listener;
        private CriticalHandle _requestQueueHandle;

        /// <summary>
        /// Initializes a new instance of <see cref="DisconnectHandler"/>.
        /// </summary>
        /// <param name="listener">The <see cref="Server"/>'s HttpListener</param>
        public DisconnectHandler(HttpListener listener)
        {
            _listener = listener;
        }

        /// <summary>
        /// Initializes the Request Queue Handler.  Meant to be called once the servers <see cref="HttpListener"/> has been started.
        /// </summary>
        public void Initialize()
        {
            // HACK: Get the request queue handle so we can register for disconnect
            var requestQueueHandleField = typeof(HttpListener).GetField("m_RequestQueueHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            if (requestQueueHandleField != null)
            {
                _requestQueueHandle = (CriticalHandle)requestQueueHandleField.GetValue(_listener);
            }
        }

        /// <summary>
        /// Gets the <see cref="CancellationToken"/> associated with the <paramref name="context"/>.  
        /// If the <see cref="CancellationToken"/> does not exist for the given <paramref name="context"/> then <see cref="CreateToken"/> is called.
        /// </summary>
        /// <param name="context">The context for the current connection.</param>
        /// <returns>A cancellation token that is registered for disconnect for the current connection.</returns>
        public CancellationToken GetDisconnectToken(HttpListenerContext context)
        {
            FieldInfo connectionIdField = typeof(HttpListenerRequest).GetField("m_ConnectionId", BindingFlags.Instance | BindingFlags.NonPublic);
            ulong connectionId = (ulong)connectionIdField.GetValue(context.Request);

            return (connectionIdField != null) ? _connectionCancellationTokens.GetOrAdd(connectionId, key => new Lazy<CancellationTokenSource>(() => CreateToken(key))).Value.Token : CancellationToken.None;
        }

        /// <summary>
        /// Creates a <see cref="CancellationTokenSource"/> for the given <paramref name="connectionId"/> and registers it for disconnect.
        /// </summary>
        /// <param name="connectionId">The connection id.</param>
        /// <returns>A <see cref="CancellationTokenSource"/> that is registered for disconnect for the connection associated with the <paramref name="connectionId"/>.</returns>
        public CancellationTokenSource CreateToken(ulong connectionId)
        {            
            if (_requestQueueHandle != null)
            {
                Debug.WriteLine("Server: Registering connection for disconnect");
                // Create a nativeOverlapped callback so we can register for disconnect callback
                var overlapped = new Overlapped();

                var nativeOverlapped = overlapped.UnsafePack((errorCode, numBytes, pOVERLAP) =>
                {
                    Debug.WriteLine("Server: http.sys disconnect callback fired.");

                    // Free the overlapped
                    Overlapped.Free(pOVERLAP);

                    // Pull the token out of the list and Cancel it.
                    Lazy<CancellationTokenSource> cts;
                    _connectionCancellationTokens.TryRemove(connectionId, out cts);
                    cts.Value.Cancel();
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
                Debug.WriteLine("Server: Unable to resolve requestQueue handle. Disconnect notifications will be ignored");
            }

            return new CancellationTokenSource();
        }
    }
}
