// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
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

        private int _startIndex = 0;

        // List of transports in fallback order
        private readonly IList<IClientTransport> _transports;

        public AutoTransport(IHttpClient httpClient)
        {
            _httpClient = httpClient;
            _transports = new IClientTransport[] { 
#if NET45
                new WebSocketTransport(httpClient),
#endif
                new ServerSentEventsTransport(httpClient), 
                new LongPollingTransport(httpClient) 
            };
        }

        public AutoTransport(IHttpClient httpClient, IList<IClientTransport> transports)
        {
            _httpClient = httpClient;
            _transports = transports;
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

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            var task = _httpClient.GetNegotiationResponse(connection);
#if NET45
            return task.Then(response =>
            {
                if (!response.TryWebSockets)
                {
                    _startIndex = 1;
                }

                return response;
            });
#else
            return task;
#endif
        }

        public Task Start(IConnection connection, string data, CancellationToken disconnectToken)
        {
            var tcs = new TaskCompletionSource<object>();

            // Resolve the transport
            ResolveTransport(connection, data, disconnectToken, tcs, _startIndex);

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
                    // Make sure we observe the exception
                    var ex = task.Exception.GetBaseException();

                    connection.Trace(TraceLevels.Events, "Auto: Failed to connect to using transport {0}. {1}", transport.Name, ex);

                    // If that transport fails to initialize then fallback
                    var next = index + 1;
                    if (next < _transports.Count)
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

            },
            TaskContinuationOptions.ExecuteSynchronously);
        }

        public Task Send(IConnection connection, string data)
        {
            return _transport.Send(connection, data);
        }

        public void Abort(IConnection connection, TimeSpan timeout)
        {
            if (_transport != null)
            {
                _transport.Abort(connection, timeout);
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
