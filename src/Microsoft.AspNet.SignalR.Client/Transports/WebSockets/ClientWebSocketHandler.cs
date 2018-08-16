// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD2_0

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

#elif NET40 || NETSTANDARD1_3
// Not supported on this framework.
#else 
#error Unsupported target framework.
#endif

