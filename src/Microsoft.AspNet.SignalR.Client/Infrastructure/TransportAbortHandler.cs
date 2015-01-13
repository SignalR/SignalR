// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class TransportAbortHandler : IDisposable
    {
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

        public virtual void Abort(IConnection connection, TimeSpan timeout, string connectionData)
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

                    var url = UrlBuilder.BuildAbort(connection, _transportName, connectionData);

                    _httpClient.Post(url, connection.PrepareRequest, isLongRunning: false)
                               .Catch((ex, state) =>
                               {
                                   // If there's an error making an http request set the reset event
                                   ((TransportAbortHandler)state).CompleteAbort();
                               },
                                   this,
                                   connection);

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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
