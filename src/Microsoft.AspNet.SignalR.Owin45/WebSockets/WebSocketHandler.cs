// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.WebSockets
{
    internal class WebSocketHandler
    {
        private static readonly TimeSpan _closeTimeout = TimeSpan.FromMilliseconds(250); // wait 250 ms before giving up on a Close
        private const int _receiveLoopBufferSize = 8 * 1024; // 8K default fragment size (we expect most messages to be very short)

        private int _maxIncomingMessageSize = 4 * 1024 * 1024; // 4MB default max incoming message size
        private readonly TaskQueue _sendQueue = new TaskQueue(); // queue for sending messages

        /*
         * USER OVERRIDES
         */

        // First method to be called when the socket becomes active; invoked 1 time per socket
        public virtual void OnOpen() { }

        // Called when a text or binary message is received; invoked 0+ times per socket
        // The developer must override the appropriate overload(s) depending on the type of message he anticipates, else the connection will close
        public virtual void OnMessage(string message) { throw new NotImplementedException(); }
        public virtual void OnMessage(byte[] message) { throw new NotImplementedException(); }

        // Called when a fault occurs on the socket; invoked 0 or 1 time per socket
        // The developer can look at the Error property to get the exception
        public virtual void OnError() { }

        // Called when the socket is closed; invoked 1 time per socket
        public virtual void OnClose(bool clean) { }

        /*
         * NON-VIRTUAL HELPER METHODS
         */

        // Sends a text message to the client
        public Task Send(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return SendAsync(message);
        }
        
        internal Task SendAsync(string message)
        {
            return SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);
        }

        internal Task SendAsync(byte[] message, WebSocketMessageType messageType)
        {
            return _sendQueue.Enqueue(() => WebSocket.SendAsync(new ArraySegment<byte>(message), messageType, true /* endOfMessage */, CancellationToken.None));
        }

        // Gracefully closes the connection
        public void Close()
        {
            CloseAsync();
        }

        internal Task CloseAsync()
        {
            return _sendQueue.Enqueue(() => WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None));
        }

        /*
         * CONFIGURATION PROPERTIES
         */

        public int MaxIncomingMessageSize
        {
            get
            {
                return _maxIncomingMessageSize;
            }
        }

        /*
         * MISC PROPERTIES
         */

        public WebSocket WebSocket { get; set; }
        public Exception Error { get; set; }

        /*
         * IMPLEMENTATION
         */

        public Task ProcessWebSocketRequestAsync(WebSocket webSocket)
        {
            if (webSocket == null)
            {
                throw new ArgumentNullException("webSocket");
            }

            byte[] buffer = new byte[_receiveLoopBufferSize];
            return ProcessWebSocketRequestAsync(webSocket, () => WebSocketMessageReader.ReadMessageAsync(webSocket, buffer, MaxIncomingMessageSize));
        }

        internal async Task ProcessWebSocketRequestAsync(WebSocket webSocket, Func<Task<WebSocketMessage>> messageRetriever)
        {
            bool cleanClose = true;
            try
            {
                // first, set primitives and initialize the object
                WebSocket = webSocket;
                OnOpen();

                // dispatch incoming messages
                while (true)
                {
                    WebSocketMessage incomingMessage = await messageRetriever();
                    switch (incomingMessage.MessageType)
                    {
                        case WebSocketMessageType.Binary:
                            OnMessage((byte[])incomingMessage.Data);
                            break;

                        case WebSocketMessageType.Text:
                            OnMessage((string)incomingMessage.Data);
                            break;

                        default:
                            // If we received an incoming CLOSE message, we'll queue a CLOSE frame to be sent.
                            // We'll give the queued frame some amount of time to go out on the wire, and if a
                            // timeout occurs we'll give up and abort the connection.
                            await Task.WhenAny(CloseAsync(), Task.Delay(_closeTimeout))
                                .ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously); // swallow exceptions occurring from sending the CLOSE
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsFatalException(ex))
                {
                    Error = ex;
                    OnError();
                    cleanClose = false;
                }
            }
            finally
            {
                try
                {
                    OnClose(cleanClose);
                }
                finally
                {
                    // call Dispose if it exists
                    IDisposable disposable = this as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
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
    }
}
