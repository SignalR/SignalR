// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : IClientTransport
    {
        public string Name
        {
            get { return "webSockets"; }
        }

        public bool SupportsKeepAlive
        {
            get { throw new NotImplementedException(); }
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
            throw new NotImplementedException();
        }
    }
}
