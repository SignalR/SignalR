// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public class WebSocketTransport : ClientTransportBase
    {
        public WebSocketTransport()
            : this(new DefaultHttpClient())
        {            
        }

        public WebSocketTransport(IHttpClient httpClient)
            : base(httpClient, "webSockets")
        {
        }

        public override bool SupportsKeepAlive
        {
            get { return true; }
        }

        public override Task Start(IConnection connection, string connectionData, CancellationToken disconnectToken)
        {
            throw new NotImplementedException();
        }

        public override Task Send(IConnection connection, string data, string connectionData)
        {
            throw new NotImplementedException();
        }

        public override void LostConnection(IConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}
