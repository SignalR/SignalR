// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : IClientTransport
    {
        private readonly IHttpClient _httpClient;
        private readonly TransportHelper _transportHelper;

        public WebSocketTransport()
            : this(new DefaultHttpClient())
        {            
        }

        public WebSocketTransport(IHttpClient httpClient)
            : this(httpClient, new TransportHelper())
        {
        }

        internal WebSocketTransport(IHttpClient httpClient, TransportHelper transportHelper)
        {
            Debug.Assert(transportHelper != null, "transportHelper is null");

            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            _httpClient = httpClient;
            _transportHelper = transportHelper;
        }

        ~WebSocketTransport()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets transport name.
        /// </summary>
        public string Name
        {
            get { return "webSockets"; }
        }

        public bool SupportsKeepAlive
        {
            get { return true; }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection, string connectionData)
        {
            return _transportHelper.GetNegotiationResponse(_httpClient, connection, connectionData);
        }

        public Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            throw new NotImplementedException();
        }

        public Task Send(IConnection connection, string data, string connectionData)
        {
            throw new NotImplementedException();
        }

        public void Abort(IConnection connection, TimeSpan timeout, string connectionData)
        {
            throw new NotImplementedException();
        }

        public void LostConnection(IConnection connection)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {   
        }
    }
}
