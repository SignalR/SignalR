// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNet.SignalR.WebSockets;

namespace Microsoft.AspNet.SignalR.Client.Transports.WebSockets
{
    internal class ClientWebSocketHandler : WebSocketHandler
    {
        private readonly WebSocketTransport _webSocketTransport;

        public ClientWebSocketHandler(WebSocketTransport webSocketTransport)
            : base(maxIncomingMessageSize: null)
        {
            Debug.Assert(webSocketTransport != null, "webSocketTransport is null");

            _webSocketTransport = webSocketTransport;
        }

        // for mocking
        internal ClientWebSocketHandler()
            : base(maxIncomingMessageSize: null)
        {
        }

        public override void OnMessage(string message)
        {
            _webSocketTransport.OnMessage(message);
        }

        public override void OnOpen()
        {
            _webSocketTransport.OnOpen();
        }

        public override void OnClose()
        {
            _webSocketTransport.OnClose();
        }

        public override void OnError()
        {
            _webSocketTransport.OnError(Error);
        }
    }
}
