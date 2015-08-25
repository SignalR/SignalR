// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.WebSockets
{
    public class DefaultWebSocketHandler : WebSocketHandler, IWebSocket
    {
        private readonly IWebSocket _webSocket;
        private volatile bool _closed;

        public DefaultWebSocketHandler(int? maxIncomingMessageSize)
            : base(maxIncomingMessageSize)
        {
            _webSocket = this;

            _webSocket.OnClose = () => { };
            _webSocket.OnError = e => { };
            _webSocket.OnMessage = msg => { };
        }

        public override void OnClose()
        {
            _closed = true;

            _webSocket.OnClose();
        }

        public override void OnError()
        {
            _webSocket.OnError(Error);
        }

        public override void OnMessage(string message)
        {
            _webSocket.OnMessage(message);
        }

        Action<string> IWebSocket.OnMessage
        {
            get;
            set;
        }

        Action IWebSocket.OnClose
        {
            get;
            set;
        }

        Action<Exception> IWebSocket.OnError
        {
            get;
            set;
        }

        public override Task Send(string message)
        {
            if (_closed)
            {
                return TaskAsyncHelper.Empty;
            }

            return base.Send(message);
        }

        public Task Send(ArraySegment<byte> message)
        {
            if (_closed)
            {
                return TaskAsyncHelper.Empty;
            }

            return base.SendAsync(message, WebSocketMessageType.Text);
        }

        public override Task CloseAsync()
        {
            if (_closed)
            {
                return TaskAsyncHelper.Empty;
            }

            return base.CloseAsync();
        }
    }
}
