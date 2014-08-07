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
        private readonly TransportAbortHandler _abortHandler;
        private bool _finished = false;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed in the Dispose method.")]
        protected ClientTransportBase(IHttpClient httpClient, string transportName)
            : this(httpClient, transportName, new TransportHelper(), new TransportAbortHandler(httpClient, transportName))
        {
        }

        internal ClientTransportBase(IHttpClient httpClient, string transportName, TransportHelper transportHelper, TransportAbortHandler abortHandler)
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
            Debug.Assert(abortHandler != null, "abortHandler is null");

            _httpClient = httpClient;
            _transportName = transportName;
            _transportHelper = transportHelper;
            _abortHandler = abortHandler;
        }

        protected IHttpClient HttpClient
        {
            get { return _httpClient; }
        }

        protected TransportHelper TransportHelper
        {
            get { return _transportHelper; }
        }

        protected TransportAbortHandler AbortHandler
        {
            get { return _abortHandler; }
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
            if(_finished)
            {
                throw new InvalidOperationException(Resources.Error_TransportCannotBeReused);
            }

            return TransportHelper.GetNegotiationResponse(HttpClient, connection, connectionData);
        }

        public abstract Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken);

        public abstract Task Send(IConnection connection, string data, string connectionData);

        public virtual void Abort(IConnection connection, TimeSpan timeout, string connectionData)
        {
            _finished = true;
            AbortHandler.Abort(connection, timeout, connectionData);
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
                _finished = true;
                _abortHandler.Dispose();
            }
        }
    }
}
