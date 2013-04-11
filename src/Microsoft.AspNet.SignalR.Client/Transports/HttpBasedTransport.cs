// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The send query string
        private const string _sendQueryString = "?transport={0}&connectionToken={1}{2}";

        // The transport name
        private readonly string _transport;

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

        protected ManualResetEvent AbortResetEvent { get; private set; }

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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want Stop to throw. IHttpClient.PostAsync could throw anything.")]
        public void Abort(IConnection connection, TimeSpan timeout)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            lock (this)
            {
                if (AbortResetEvent == null)
                {
                    AbortResetEvent = new ManualResetEvent(initialState: false);

                    string url = connection.Url + "abort" + String.Format(CultureInfo.InvariantCulture,
                                                                          _sendQueryString,
                                                                          _transport,
                                                                          Uri.EscapeDataString(connection.ConnectionToken),
                                                                          null);

                    url += TransportHelper.AppendCustomQueryString(connection, url);

                    _httpClient.Post(url, connection.PrepareRequest).Catch((ex, state) =>
                    {
                        // If there's an error making an http request set the reset event
                        ((ManualResetEvent)state).Set();
                    },
                    AbortResetEvent);
                }
            }

            if (!AbortResetEvent.WaitOne(timeout))
            {
                connection.Trace(TraceLevels.Events, "Abort never fired");
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
                if (AbortResetEvent != null)
                {
                    AbortResetEvent.Dispose();
                }
            }
        }
    }
}
