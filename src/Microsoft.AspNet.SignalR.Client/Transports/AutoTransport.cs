// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class AutoTransport : IClientTransport
    {
        // Transport that's in use
        private IClientTransport _transport;

        private readonly IHttpClient _httpClient;

        // List of transports in fallback order
        private readonly IClientTransport[] _transports;

        public AutoTransport(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _transports = new IClientTransport[] { new ServerSentEventsTransport(httpClient), new LongPollingTransport(httpClient) };
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return HttpBasedTransport.GetNegotiationResponse(_httpClient, connection);
        }

        public Task Start(IConnection connection, string data, CancellationToken disconnectToken)
        {
            var tcs = new TaskCompletionSource<object>();

            // Resolve the transport
            ResolveTransport(connection, data, disconnectToken, tcs, 0);

            return tcs.Task;
        }

        private void ResolveTransport(IConnection connection, string data, CancellationToken disconnectToken, TaskCompletionSource<object> tcs, int index)
        {
            // Pick the current transport
            IClientTransport transport = _transports[index];

            transport.Start(connection, data, disconnectToken).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
#if !WINDOWS_PHONE && !SILVERLIGHT && !NETFX_CORE
                    // Make sure we observe the exception
                    var ex = task.Exception;
                    Trace.TraceError("SignalR exception thrown by Task: {0}", ex);
#endif
#if NET35
                    Debug.WriteLine(System.String.Format(CultureInfo.InvariantCulture, "Auto: Failed to connect to using transport {0}", (object)transport.GetType().Name));
#else
                    Debug.WriteLine("Auto: Failed to connect to using transport {0}", (object)transport.GetType().Name);
#endif

                    // If that transport fails to initialize then fallback
                    var next = index + 1;
                    if (next < _transports.Length)
                    {
                        // Try the next transport
                        ResolveTransport(connection, data, disconnectToken, tcs, next);
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

        public Task<T> Send<T>(IConnection connection, string data)
        {
            return _transport.Send<T>(connection, data);
        }

        public void Abort(IConnection connection)
        {
            if (_transport != null)
            {
                _transport.Abort(connection);
            }
        }
    }
}
