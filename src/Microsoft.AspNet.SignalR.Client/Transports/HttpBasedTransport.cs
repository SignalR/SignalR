// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The send query string
        private const string _sendQueryString = "?transport={0}&connectionToken={1}{2}";

        // The transport name
        private readonly string _transport;

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

        private readonly IHttpClient _httpClient;

        protected HttpBasedTransport(IHttpClient httpClient, string transport)
        {
            _httpClient = httpClient;
            _transport = transport;
        }

        public string Name
        {
            get
            {
                return _transport;
            }
        }

        /// <summary>
        /// Indicates whether or not the transport supports keep alive
        /// </summary>
        public abstract bool SupportsKeepAlive { get; }

        protected IHttpClient HttpClient
        {
            get { return _httpClient; }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return _httpClient.GetNegotiationResponse(connection);
        }

        public Task Start(IConnection connection, string data, CancellationToken disconnectToken)
        {
            var tcs = new TaskCompletionSource<object>();

            OnStart(connection, data, disconnectToken, () => tcs.TrySetResult(null), exception => tcs.TrySetException(exception));

            return tcs.Task;
        }

        protected abstract void OnStart(IConnection connection,
                                        string data,
                                        CancellationToken disconnectToken,
                                        Action initializeCallback,
                                        Action<Exception> errorCallback);

        public Task Send(IConnection connection, string data)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            string url = connection.Url + "send";
            string customQueryString = String.IsNullOrEmpty(connection.QueryString) ? String.Empty : "&" + connection.QueryString;

            url += String.Format(CultureInfo.InvariantCulture,
                                _sendQueryString,
                                _transport,
                                Uri.EscapeDataString(connection.ConnectionToken),
                                customQueryString);

            var postData = new Dictionary<string, string> {
                { "data", data }
            };

            return _httpClient.Post(url, connection.PrepareRequest, postData)
                              .Then(response => response.ReadAsString())
                              .Then(raw =>
                              {
                                  connection.Trace(TraceLevels.Messages, "OnMessage({0})", raw);

                                  if (!String.IsNullOrEmpty(raw))
                                  {
                                      connection.OnReceived(JObject.Parse(raw));
                                  }
                              })
                              .Catch(connection.OnError);
        }

        public void Abort(IConnection connection, TimeSpan timeout)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            // Abort should never complete before any of its previous calls
            lock (_abortLock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                // Ensure that an abort request is only made once
                if (!_startedAbort)
                {
                    _startedAbort = true;

                    string url = connection.Url + "abort" + String.Format(CultureInfo.InvariantCulture,
                                                                          _sendQueryString,
                                                                          _transport,
                                                                          Uri.EscapeDataString(connection.ConnectionToken),
                                                                          null);

                    url += TransportHelper.AppendCustomQueryString(connection, url);

                    _httpClient.Post(url, connection.PrepareRequest).Catch((ex, state) =>
                    {
                        // If there's an error making an http request set the reset event
                        ((HttpBasedTransport)state).CompleteAbort();
                    },
                    this);

                    if (!_abortResetEvent.WaitOne(timeout))
                    {
                        connection.Trace(TraceLevels.Events, "Abort never fired");
                    }
                }
            }
        }

        protected void CompleteAbort()
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

        protected bool TryCompleteAbort()
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

        protected string GetReceiveQueryString(IConnection connection, string data)
        {
            return TransportHelper.GetReceiveQueryString(connection, data, _transport);
        }

        public abstract void LostConnection(IConnection connection);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Wait for any ongoing aborts to complete
                // In practice, any aborts should have finished by the time Dispose is called
                lock (_abortLock)
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
