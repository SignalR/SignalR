// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The transport name
        private readonly string _transport;

        private readonly IHttpClient _httpClient;
        private readonly TransportAbortHandler _abortHandler;

        private NegotiationResponse _negotiationResponse;

        // Not meant to be used outside of this class hierarchy. This should be 
        // "private protected" but this is supported only in C# 6.0.
        internal readonly UrlBuilder _urlBuilder  = new UrlBuilder();
        internal readonly TransportHelper _transportHelper = new TransportHelper();

        protected HttpBasedTransport(IHttpClient httpClient, string transport)
        {
            _httpClient = httpClient;
            _transport = transport;
            _abortHandler = new TransportAbortHandler(httpClient, transport);
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

        protected TransportAbortHandler AbortHandler
        {
            get { return _abortHandler; }
        }

        protected NegotiationResponse NegotiationResponse
        {
            get { return _negotiationResponse; }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection, string connectionData)
        {
            Task<NegotiationResponse> response = _transportHelper.GetNegotiationResponse(_httpClient, connection, connectionData);

            response.Then(negotiationResponse => _negotiationResponse = negotiationResponse);

            return response;
        }

        public Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var initializeHandler = new TransportInitializationHandler(_httpClient, connection, connectionData, Name, disconnectToken, _transportHelper);

            OnStart(connection, connectionData, disconnectToken, initializeHandler);

            return initializeHandler.Task;
        }

        protected abstract void OnStart(IConnection connection,
                                        string connectionData,
                                        CancellationToken disconnectToken,
                                        TransportInitializationHandler initializeHandler);

        public Task Send(IConnection connection, string data, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            string url = _urlBuilder.BuildSend(connection, Name, connectionData);

            var postData = new Dictionary<string, string> {
                { "data", data }
            };

            return _httpClient.Post(url, connection.PrepareRequest, postData, isLongRunning: false)
                              .Then(response => response.ReadAsString())
                              .Then(raw =>
                              {
                                  if (!String.IsNullOrEmpty(raw))
                                  {
                                      connection.Trace(TraceLevels.Messages, "OnMessage({0})", raw);

                                      connection.OnReceived(connection.JsonDeserializeObject<JObject>(raw));
                                  }
                              })
                              .Catch(connection.OnError);
        }

        public void Abort(IConnection connection, TimeSpan timeout, string connectionData)
        {
            _abortHandler.Abort(connection, timeout, connectionData);
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
                _abortHandler.Dispose();
            }
        }
    }
}
