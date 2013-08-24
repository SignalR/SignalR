// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.WebSockets
{
    internal class DefaultWebSocketHandler : WebSocketHandler, IWebSocket
    {
        // 64KB default max incoming message size
        private const int _maxIncomingMessageSize = 64 * 1024;
        private static readonly byte[] _zeroByteBuffer = new byte[0];
        private readonly IWebSocket _webSocket;
        private volatile bool _closed;

        public DefaultWebSocketHandler()
            : base(_maxIncomingMessageSize)
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

        Task IWebSocket.Send(string value)
        {
            return Send(value);
        }

        public override Task Send(string message)
        {
            if (_closed)
            {
                return TaskAsyncHelper.Empty;
            }

            return base.Send(message);
        }

        public override Task CloseAsync()
        {
            if (_closed)
            {
                return TaskAsyncHelper.Empty;
            }

            return base.CloseAsync();
        }

        public Task SendChunk(ArraySegment<byte> message)
        {
            if (_closed)
            {
                return TaskAsyncHelper.Empty;
            }

            return SendAsync(message, WebSocketMessageType.Text, endOfMessage: false);
        }

        public Task Flush()
        {
            if (_closed)
            {
                return TaskAsyncHelper.Empty;
            }

            return SendAsync(new ArraySegment<byte>(_zeroByteBuffer), WebSocketMessageType.Text, endOfMessage: true);
        }
    }
}
