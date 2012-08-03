using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SignalR.Hosting.Self.Infrastructure;

namespace SignalR.Hosting.Self
{
    public unsafe class DisconnectHandler
    {
        private readonly ConcurrentDictionary<ulong, Lazy<CancellationTokenSource>> _connectionCancellationTokens = new ConcurrentDictionary<ulong, Lazy<CancellationTokenSource>>();
        private readonly HttpListener _listener;
        private CriticalHandle _requestQueueHandle;

        public DisconnectHandler(HttpListener listener)
        {
            _listener = listener;
        }

        public void Initialize()
        {
            // HACK: Get the request queue handle so we can register for disconnect
            var requestQueueHandleField = typeof(HttpListener).GetField("m_RequestQueueHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            if (requestQueueHandleField != null)
            {
                _requestQueueHandle = (CriticalHandle)requestQueueHandleField.GetValue(_listener);
            }
        }

        public CancellationToken GetOrAddDisconnectToken(HttpListenerContext context)
        {
            FieldInfo connectionIdField = typeof(HttpListenerRequest).GetField("m_ConnectionId", BindingFlags.Instance | BindingFlags.NonPublic);
            ulong connectionId = (ulong)connectionIdField.GetValue(context.Request);

            return _connectionCancellationTokens.GetOrAdd(connectionId, key => CreateToken(key, connectionIdField)).Value.Token;
        }

        public Lazy<CancellationTokenSource> CreateToken(ulong connectionId, FieldInfo connectionIdField)
        {            
            if (_requestQueueHandle != null && connectionIdField != null)
            {
                Debug.WriteLine("Server: Registering connection for disconnect");
                // Create a nativeOverlapped callback so we can register for disconnect callback
                var overlapped = new Overlapped();
                Lazy<CancellationTokenSource> cts = new Lazy<CancellationTokenSource>();

                var nativeOverlapped = overlapped.UnsafePack((errorCode, numBytes, pOVERLAP) =>
                {
                    Debug.WriteLine("Server: http.sys disconnect callback fired.");

                    // Free the overlapped
                    Overlapped.Free(pOVERLAP);

                    // Pull the token out of the list and Cancel it.
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

                return cts;
            }
            else
            {
                Debug.WriteLine("Server: Unable to resolve requestQueue handle. Disconnect notifications will be ignored");
                return new Lazy<CancellationTokenSource>();
            }
        }
    }
}
