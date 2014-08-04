// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Infrastructure;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class ClientTransportBase : IClientTransport
    {
        private readonly IHttpClient _httpClient;
        private readonly string _transportName;
        private readonly TransportHelper _transportHelper;
        private int _finished = 0;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in the Dispose method.")]
        protected ClientTransportBase(IHttpClient httpClient, string transportName)
            : this(httpClient, transportName, new TransportHelper())
        {
        }

        internal ClientTransportBase(IHttpClient httpClient, string transportName, TransportHelper transportHelper)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            if (string.IsNullOrWhiteSpace(transportName))
            {
                throw new ArgumentNullException("transportName");
            }

            Debug.Assert(transportHelper != null, "transportHelper is null");

            _httpClient = httpClient;
            _transportName = transportName;
            _transportHelper = transportHelper;
        }

        protected IHttpClient HttpClient
        {
            get { return _httpClient; }
        }

        protected TransportHelper TransportHelper
        {
            get { return _transportHelper; } 
        }

        /// <summary>
        /// Gets transport name.
        /// </summary>
        public string Name
        {
            get { return _transportName; }
        }

        public abstract bool SupportsKeepAlive { get; }

        public virtual Task<NegotiationResponse> Negotiate(IConnection connection, string connectionData)
        {
            if (Finished)
            {
                throw new InvalidOperationException(Resources.Error_TransportCannotBeReused);
            }

            return TransportHelper.GetNegotiationResponse(HttpClient, connection, connectionData);
        }

        public abstract Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken);

        public abstract Task Send(IConnection connection, string data, string connectionData);

        public virtual void Abort(IConnection connection, string connectionData)
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
                _finished = 1;
                return;
            }

            if (Interlocked.Exchange(ref _finished, 1) == 0)
            {
                var url = UrlBuilder.BuildAbort(connection, _transportName, connectionData);

                _httpClient.Post(url, connection.PrepareRequest, isLongRunning: false)
                    .Catch(
                        (ex, state) => connection.Trace(TraceLevels.Messages, "Sending abort failed due to {0}", ex),
                        this,
                        connection);
            }
        }

        // internal for testing
        protected internal bool Finished
        {
            get { return _finished != 0; }
        }

        public abstract void LostConnection(IConnection connection);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _finished = 1;
        }
    }
}
