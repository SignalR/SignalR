// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public sealed class TransportAbortHandler : IDisposable
    {
        // The abort query string
        private const string _abortQueryString = "?transport={0}&clientProtocol={1}&connectionData={2}&connectionToken={3}{4}";

        // The transport name
        private readonly string _transportName;

        private readonly IHttpClient _httpClient;

        // Used to complete the synchronous call to Abort()
        private ManualResetEvent _abortResetEvent = new ManualResetEvent(initialState: false);

        // Used to indicate whether Abort() has been called
        private bool _startedAbort;
        // Used to ensure that Abort() runs effectively only once
        // The _abortLock subsumes the _disposeLock and can be held upwards of 30 seconds
        private readonly object _abortLock = new object();

        // Used to ensure the _abortResetEvent.Set() isn't called after disposal
        private bool _disposed;
        // Used to make checking _disposed and calling _abortResetEvent.Set() thread safe
        private readonly object _disposeLock = new object();

        public TransportAbortHandler(IHttpClient httpClient, string transportName)
        {
            _httpClient = httpClient;
            _transportName = transportName;
        }

        public void Abort(IConnection connection, TimeSpan timeout, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            // Save the connection.ConnectionToken since race issue that connection.ConnectionToken can be set to null in different thread
            var connectionToken = connection.ConnectionToken;

            if (connectionToken == null)
            {
                connection.Trace(TraceLevels.Messages, "Connection already disconnected, skipping abort.");
                return;
            }

            // Abort should never complete before any of its previous calls
            lock (_abortLock)
            {
                if (_disposed)
                {
                    return;
                }

                // Ensure that an abort request is only made once
                if (!_startedAbort)
                {
                    _startedAbort = true;

                    string url = connection.Url + "abort" + String.Format(CultureInfo.InvariantCulture,
                                                                          _abortQueryString,
                                                                          _transportName,
                                                                          connection.Protocol,
                                                                          connectionData,
                                                                          Uri.EscapeDataString(connectionToken),
                                                                          null);

                    url += TransportHelper.AppendCustomQueryString(connection, url);

                    _httpClient.Post(url, connection.PrepareRequest, isLongRunning: false).Catch((ex, state) =>
                    {
                        // If there's an error making an http request set the reset event
                        ((TransportAbortHandler)state).CompleteAbort();
                    },
                    this);

                    if (!_abortResetEvent.WaitOne(timeout))
                    {
                        connection.Trace(TraceLevels.Events, "Abort never fired");
                    }
                }
            }
        }

        public void CompleteAbort()
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    // Make any future calls to Abort() no-op
                    // Abort might still run, but any ongoing aborts will immediately complete
                    _startedAbort = true;
                    // Ensure any ongoing calls to Abort() complete
                    _abortResetEvent.Set();
                }
            }
        }

        public bool TryCompleteAbort()
        {
            // Make sure we don't Set a disposed ManualResetEvent
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    // Don't try to continue receiving messages if the transport is disposed
                    return true;
                }
                else if (_startedAbort)
                {
                    _abortResetEvent.Set();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            // Wait for any ongoing aborts to complete
            // In practice, any aborts should have finished by the time Dispose is called
            lock (_abortLock)
            {
                lock (_disposeLock)
                {
                    if (!_disposed)
                    {
                        _abortResetEvent.Dispose();
                        _disposed = true;
                    }
                }
            }
        }
    }
}
