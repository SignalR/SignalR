// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class AutoTransport : IClientTransport
    {
        private readonly IHttpClient _httpClient;

        // List of transports in fallback order
        private readonly List<IClientTransport> _transports;

        // Transport that's in use
        private IClientTransport _transport;
        private bool _tryWebSockets = true;

        public AutoTransport(IHttpClient httpClient)
        {
            _httpClient = httpClient;

            _transports = new List<IClientTransport>()
            {
#if NET45 || NETSTANDARD2_0
                new WebSocketTransport(httpClient),
#elif NET40 || NETSTANDARD1_3
                // WebSockets not supported
#else
#error Unsupported target framework.
#endif
                new ServerSentEventsTransport(httpClient),
                new LongPollingTransport(httpClient)
            };
        }

        public AutoTransport(IHttpClient httpClient, IList<IClientTransport> transports)
        {
            _httpClient = httpClient;
            _transports = new List<IClientTransport>(transports);
        }

        /// <summary>
        /// Indicates whether or not the active transport supports keep alive
        /// </summary>
        public bool SupportsKeepAlive
        {
            get
            {
                return _transport != null ? _transport.SupportsKeepAlive : false;
            }
        }

        public string Name
        {
            get
            {
                if (_transport == null)
                {
                    return null;
                }

                return _transport.Name;
            }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection, string connectionData)
        {
            var task = GetNegotiateResponse(connection, connectionData);
#if NET45 || NETSTANDARD2_0
            return task.Then(response =>
            {
                _tryWebSockets = response.TryWebSockets;
                return response;
            });
#else
            return task;
#endif
        }

        public virtual Task<NegotiationResponse> GetNegotiateResponse(IConnection connection, string connectionData)
        {
            return new TransportHelper().GetNegotiationResponse(_httpClient, connection, connectionData);
        }

        public Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            var tcs = new DispatchingTaskCompletionSource<object>();

            // Resolve the transport
            ResolveTransport(connection, connectionData, disconnectToken, tcs, lastError: null, index: 0);

            return tcs.Task;
        }

        private void ResolveTransport(
            IConnection connection,
            string data,
            CancellationToken disconnectToken,
            DispatchingTaskCompletionSource<object> tcs,
            Exception lastError,
            int index)
        {
            IClientTransport transport = null;
            var next = index;

            // Pick the current transport
            while (next < _transports.Count && transport == null)
            {
                var candidateTransport = _transports[next++];

                // If the current transport is WebSockets and it's not supported by the server, try the next transport.
                if (_tryWebSockets || !"webSockets".Equals(candidateTransport.Name, StringComparison.Ordinal))
                {
                    transport = candidateTransport;
                }
            }

            if (transport is null)
            {
                // If there's nothing else to try, then just fail with the error from the last transport.
                // For the lastError to be null, the AutoTransport must have been initialized with an empty transport list or only WebSockets.
                tcs.TrySetException(lastError ?? new Exception(Resources.Error_NoCompatibleTransportFound));
                return;
            }

            transport.Start(connection, data, disconnectToken).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Exception ex;
                    if (task.IsCanceled)
                    {
                        ex = new OperationCanceledException(Resources.Error_TaskCancelledException);
                    }
                    else
                    {
                        ex = task.Exception.GetBaseException();
                    }

                    connection.Trace(TraceLevels.Events, "Auto: Failed to connect to using transport {0}. {1}", transport.Name, ex);

                    // If that transport fails to initialize, then fallback.
                    // If it is that /start request that failed, do not fallback.
                    if (ex is StartException)
                    {
                        tcs.TrySetException(ex);
                    }
                    else
                    {
                        // Try the next transport
                        ResolveTransport(connection, data, disconnectToken, tcs, ex, next);
                    }
                }
                else
                {
                    // Set the active transport
                    _transport = transport;

                    // Complete the process
                    tcs.TrySetResult(null);
                }

            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        public Task Send(IConnection connection, string data, string connectionData)
        {
            return _transport.Send(connection, data, connectionData);
        }

        public void Abort(IConnection connection, TimeSpan timeout, string connectionData)
        {
            if (_transport != null)
            {
                _transport.Abort(connection, timeout, connectionData);
            }
        }

        public void LostConnection(IConnection connection)
        {
            _transport.LostConnection(connection);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_transport != null)
                {
                    _transport.Dispose();
                }
            }
        }
    }
}
