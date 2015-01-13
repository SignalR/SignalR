// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.WebSockets
{
    public abstract class WebSocketHandler
    {
        // Wait 250 ms before giving up on a Close
        private static readonly TimeSpan _closeTimeout = TimeSpan.FromMilliseconds(250);

        // 4KB default fragment size (we expect most messages to be very short)
        private const int _receiveLoopBufferSize = 4 * 1024;
        private readonly int? _maxIncomingMessageSize;

        // Queue for sending messages
        private readonly TaskQueue _sendQueue = new TaskQueue();

        protected WebSocketHandler(int? maxIncomingMessageSize)
        {
            _maxIncomingMessageSize = maxIncomingMessageSize;
        }

        public virtual void OnOpen() { }

        public virtual void OnMessage(string message) { throw new NotImplementedException(); }

        public virtual void OnMessage(byte[] message) { throw new NotImplementedException(); }

        public virtual void OnError() { }

        public virtual void OnClose() { }

        // Sends a text message to the client
        public virtual Task Send(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return SendAsync(message);
        }

        internal Task SendAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            return SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text);
        }

        internal virtual Task SendAsync(ArraySegment<byte> message, WebSocketMessageType messageType, bool endOfMessage = true)
        {
            if (WebSocket.State != WebSocketState.Open)
            {
                return TaskAsyncHelper.Empty;
            }

            var sendContext = new SendContext(this, message, messageType, endOfMessage);

            return _sendQueue.Enqueue(async state =>
            {
                var context = (SendContext)state;

                if (context.Handler.WebSocket.State != WebSocketState.Open)
                {
                    return;
                }

                try
                {
                    await context.Handler.WebSocket
                          .SendAsync(context.Message, context.MessageType, context.EndOfMessage, CancellationToken.None)
                          .PreserveCulture();
                }
                catch (Exception ex)
                {
                    // Swallow exceptions on send
                    Trace.TraceError("Error while sending: " + ex);
                }
            },
            sendContext);
        }

        public virtual Task CloseAsync()
        {
            if (IsClosedOrClosedSent(WebSocket))
            {
                return TaskAsyncHelper.Empty;
            }

            var closeContext = new CloseContext(this);

            return _sendQueue.Enqueue(async state =>
            {
                var context = (CloseContext)state;

                if (IsClosedOrClosedSent(context.Handler.WebSocket))
                {
                    return;
                }

                try
                {
                    await context.Handler.WebSocket
                        .CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)
                        .PreserveCulture();
                }
                catch (Exception ex)
                {
                    // Swallow exceptions on close
                    Trace.TraceError("Error while closing the websocket: " + ex);
                }
            },
            closeContext);
        }

        public int? MaxIncomingMessageSize
        {
            get
            {
                return _maxIncomingMessageSize;
            }
        }

        internal WebSocket WebSocket { get; set; }

        public Exception Error { get; set; }

        internal Task ProcessWebSocketRequestAsync(WebSocket webSocket, CancellationToken disconnectToken)
        {
            if (webSocket == null)
            {
                throw new ArgumentNullException("webSocket");
            }

            var receiveContext = new ReceiveContext(webSocket, disconnectToken, MaxIncomingMessageSize, _receiveLoopBufferSize);

            return ProcessWebSocketRequestAsync(webSocket, disconnectToken, state =>
            {
                var context = (ReceiveContext)state;

                return WebSocketMessageReader.ReadMessageAsync(context.WebSocket, context.BufferSize, context.MaxIncomingMessageSize, context.DisconnectToken);
            },
            receiveContext);
        }

        internal async Task ProcessWebSocketRequestAsync(WebSocket webSocket, CancellationToken disconnectToken, Func<object, Task<WebSocketMessage>> messageRetriever, object state)
        {
            bool closedReceived = false;

            try
            {
                // first, set primitives and initialize the object
                WebSocket = webSocket;
                OnOpen();

                // dispatch incoming messages
                while (!disconnectToken.IsCancellationRequested && !closedReceived)
                {
                    WebSocketMessage incomingMessage = await messageRetriever(state).PreserveCulture();
                    switch (incomingMessage.MessageType)
                    {
                        case WebSocketMessageType.Binary:
                            OnMessage((byte[])incomingMessage.Data);
                            break;

                        case WebSocketMessageType.Text:
                            OnMessage((string)incomingMessage.Data);
                            break;

                        default:
                            closedReceived = true;

                            // If we received an incoming CLOSE message, we'll queue a CLOSE frame to be sent.
                            // We'll give the queued frame some amount of time to go out on the wire, and if a
                            // timeout occurs we'll give up and abort the connection.
                            await Task.WhenAny(CloseAsync(), Task.Delay(_closeTimeout)).PreserveCulture();
                            break;
                    }
                }

            }
            catch (OperationCanceledException ex)
            {
                // ex.CancellationToken never has the token that was actually cancelled
                if (!disconnectToken.IsCancellationRequested)
                {
                    Error = ex;
                    OnError();
                }
            }
            catch (ObjectDisposedException)
            {
                // If the websocket was disposed while we were reading then noop
            }
            catch (Exception ex)
            {
                if (IsFatalException(ex))
                {
                    Error = ex;
                    OnError();
                }
            }

            try
            {
#if CLIENT_NET45
                if (WebSocket.State == WebSocketState.Closed ||
                    WebSocket.State == WebSocketState.Aborted)
                {
                    // No-op if the socket is already closed or aborted
                }
                else
                {
                    // Initiate the WebSocket closing handshake. Only the client should ever do this.
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).PreserveCulture();
                }
#endif
            }
            finally
            {
#if CLIENT_NET45
                WebSocket.Dispose();
#endif
                OnClose();
            }
        }

        // returns true if this is a fatal exception (e.g. OnError should be called)
        private static bool IsFatalException(Exception ex)
        {
            // If this exception is due to the underlying TCP connection going away, treat as a normal close
            // rather than a fatal exception.
            COMException ce = ex as COMException;
            if (ce != null)
            {
                switch ((uint)ce.ErrorCode)
                {
                    // These are the three error codes we've seen in testing which can be caused by the TCP connection going away unexpectedly.
                    case 0x800703e3:
                    case 0x800704cd:
                    case 0x80070026:
                        return false;
                }
            }

            // unknown exception; treat as fatal
            return true;
        }

        private static bool IsClosedOrClosedSent(WebSocket webSocket)
        {
            return webSocket.State == WebSocketState.Closed ||
                   webSocket.State == WebSocketState.CloseSent ||
                   webSocket.State == WebSocketState.Aborted;
        }

        private class CloseContext
        {
            public WebSocketHandler Handler;

            public CloseContext(WebSocketHandler webSocketHandler)
            {
                Handler = webSocketHandler;
            }
        }

        private class SendContext
        {
            public WebSocketHandler Handler;
            public ArraySegment<byte> Message;
            public WebSocketMessageType MessageType;
            public bool EndOfMessage;

            public SendContext(WebSocketHandler webSocketHandler, ArraySegment<byte> message, WebSocketMessageType messageType, bool endOfMessage)
            {
                Handler = webSocketHandler;
                Message = message;
                MessageType = messageType;
                EndOfMessage = endOfMessage;
            }
        }

        private class ReceiveContext
        {
            public WebSocket WebSocket;
            public CancellationToken DisconnectToken;
            public int? MaxIncomingMessageSize;
            public int BufferSize;

            public ReceiveContext(WebSocket webSocket, CancellationToken disconnectToken, int? maxIncomingMessageSize, int bufferSize)
            {
                WebSocket = webSocket;
                DisconnectToken = disconnectToken;
                MaxIncomingMessageSize = maxIncomingMessageSize;
                BufferSize = bufferSize;
            }
        }
    }
}
