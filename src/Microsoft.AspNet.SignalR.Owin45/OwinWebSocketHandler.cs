// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.WebSockets;

namespace Microsoft.AspNet.SignalR.Owin
{
    using WebSocketCloseAsync =
                Func
                <
                    int /* closeStatus */,
                    string /* closeDescription */,
                    CancellationToken /* cancel */,
                    Task
                >;
    using WebSocketReceiveAsync =
                        Func
                        <
                            ArraySegment<byte> /* data */,
                            CancellationToken /* cancel */,
                            Task
                            <
                                Tuple
                                <
                                    int /* messageType */,
                                    bool /* endOfMessage */,
                                    int /* count */
                                >
                            >
                        >;
    using WebSocketSendAsync =
                           Func
                           <
                               ArraySegment<byte> /* data */,
                               int /* messageType */,
                               bool /* endOfMessage */,
                               CancellationToken /* cancel */,
                               Task
                           >;

    internal class OwinWebSocketHandler
    {
        private readonly Func<IWebSocket, Task> _callback;

        public OwinWebSocketHandler(Func<IWebSocket, Task> callback)
        {
            _callback = callback;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The websocket handler disposes the socket when the receive loop is over.")]
        public Task ProcessRequestAsync(IDictionary<string, object> env)
        {
            object value;
            WebSocket webSocket;

            // Try to get the websocket context from the environment
            if (!env.TryGetValue(typeof(WebSocketContext).FullName, out value))
            {
                webSocket = new OwinWebSocket(env);
            }
            else
            {
                webSocket = ((WebSocketContext)value).WebSocket;
            }

            var handler = new DefaultWebSocketHandler();
            var task = handler.ProcessWebSocketRequestAsync(webSocket, CancellationToken.None);

            _callback(handler).Catch()
                              .Finally(state => ((DefaultWebSocketHandler)state).End(), handler);

            return task;
        }

        private class OwinWebSocket : WebSocket
        {
            private readonly WebSocketSendAsync _sendAsync;
            private readonly WebSocketReceiveAsync _receiveAsync;
            private readonly WebSocketCloseAsync _closeAsync;

            public OwinWebSocket(IDictionary<string, object> env)
            {
                _sendAsync = (WebSocketSendAsync)env[WebSocketConstants.WebSocketSendAsyncKey];
                _receiveAsync = (WebSocketReceiveAsync)env[WebSocketConstants.WebSocketReceiveAyncKey];
                _closeAsync = (WebSocketCloseAsync)env[WebSocketConstants.WebSocketCloseAsyncKey];
            }

            public override void Abort()
            {

            }

            public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                return _closeAsync((int)closeStatus, statusDescription, cancellationToken);
            }

            public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                return CloseAsync(closeStatus, statusDescription, cancellationToken);
            }

            public override WebSocketCloseStatus? CloseStatus
            {
                get { throw new NotImplementedException(); }
            }

            public override string CloseStatusDescription
            {
                get { throw new NotImplementedException(); }
            }

            public override void Dispose()
            {
            }

            public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                var tuple = await _receiveAsync(buffer, cancellationToken);

                int messageType = tuple.Item1;
                bool endOfMessage = tuple.Item2;
                int count = tuple.Item3;

                return new WebSocketReceiveResult(count, OpCodeToEnum(messageType), endOfMessage);
            }

            public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            {
                return _sendAsync(buffer, EnumToOpCode(messageType), endOfMessage, cancellationToken);
            }

            public override WebSocketState State
            {
                get { throw new NotImplementedException(); }
            }

            public override string SubProtocol
            {
                get { throw new NotImplementedException(); }
            }

            private static WebSocketMessageType OpCodeToEnum(int messageType)
            {
                switch (messageType)
                {
                    case 0x1:
                        return WebSocketMessageType.Text;
                    case 0x2:
                        return WebSocketMessageType.Binary;
                    case 0x8:
                        return WebSocketMessageType.Close;
                    default:
                        throw new ArgumentOutOfRangeException("messageType", messageType, String.Empty);
                }
            }

            private static int EnumToOpCode(WebSocketMessageType webSocketMessageType)
            {
                switch (webSocketMessageType)
                {
                    case WebSocketMessageType.Text:
                        return 0x1;
                    case WebSocketMessageType.Binary:
                        return 0x2;
                    case WebSocketMessageType.Close:
                        return 0x8;
                    default:
                        throw new ArgumentOutOfRangeException("webSocketMessageType", webSocketMessageType, String.Empty);
                }
            }
        }
    }
}
