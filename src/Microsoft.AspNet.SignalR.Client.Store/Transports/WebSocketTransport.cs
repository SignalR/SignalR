// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.WebSockets;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : WebSocketHandler, IClientTransport
    {
        public WebSocketTransport()
            : base(null)
        {
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
            throw new NotImplementedException();
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

        public override void OnOpen()
        {
            throw new NotImplementedException();
        }

        public override void OnMessage(string message)
        {
            throw new NotImplementedException();
        }

        public override void OnMessage(byte[] message)
        {
            throw new NotImplementedException();
        }

        public override void OnError()
        {
            throw new NotImplementedException();
        }

        public override void OnClose()
        {
            throw new NotImplementedException();
        }
    }
}
