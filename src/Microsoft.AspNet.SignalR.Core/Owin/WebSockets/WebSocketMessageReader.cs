// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.WebSockets
{
    internal static class WebSocketMessageReader
    {
        private static readonly ArraySegment<byte> _emptyArraySegment = new ArraySegment<byte>(new byte[0]);

        private static byte[] BufferSliceToByteArray(byte[] buffer, int count)
        {
            byte[] newArray = new byte[count];
            Buffer.BlockCopy(buffer, 0, newArray, 0, count);
            return newArray;
        }

        private static string BufferSliceToString(byte[] buffer, int count)
        {
            return Encoding.UTF8.GetString(buffer, 0, count);
        }

        public static async Task<WebSocketMessage> ReadMessageAsync(WebSocket webSocket, int bufferSize, int? maxMessageSize, CancellationToken disconnectToken)
        {
            WebSocketMessage message;

            // Read the first time with an empty array
            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(_emptyArraySegment, disconnectToken).PreserveCultureNotContext();

            if (TryGetMessage(receiveResult, null, out message))
            {
                return message;
            }

            var buffer = new byte[bufferSize];

            // Now read with the real buffer
            var arraySegment = new ArraySegment<byte>(buffer);

            receiveResult = await webSocket.ReceiveAsync(arraySegment, disconnectToken).PreserveCultureNotContext();

            if (TryGetMessage(receiveResult, buffer, out message))
            {
                return message;
            }
            else
            {
                // for multi-fragment messages, we need to coalesce
                ByteBuffer bytebuffer = new ByteBuffer(maxMessageSize);
                bytebuffer.Append(BufferSliceToByteArray(buffer, receiveResult.Count));
                WebSocketMessageType originalMessageType = receiveResult.MessageType;

                while (true)
                {
                    // loop until an error occurs or we see EOF
                    receiveResult = await webSocket.ReceiveAsync(arraySegment, disconnectToken).PreserveCultureNotContext();

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return WebSocketMessage.CloseMessage;
                    }

                    if (receiveResult.MessageType != originalMessageType)
                    {
                        throw new InvalidOperationException("Incorrect message type");
                    }

                    bytebuffer.Append(BufferSliceToByteArray(buffer, receiveResult.Count));

                    if (receiveResult.EndOfMessage)
                    {
                        switch (receiveResult.MessageType)
                        {
                            case WebSocketMessageType.Binary:
                                return new WebSocketMessage(bytebuffer.GetByteArray(), WebSocketMessageType.Binary);

                            case WebSocketMessageType.Text:
                                return new WebSocketMessage(bytebuffer.GetString(), WebSocketMessageType.Text);

                            default:
                                throw new InvalidOperationException("Unknown message type");
                        }
                    }
                }
            }
        }

        private static bool TryGetMessage(WebSocketReceiveResult receiveResult, byte[] buffer, out WebSocketMessage message)
        {
            message = null;

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                message = WebSocketMessage.CloseMessage;
            }
            else if (receiveResult.EndOfMessage)
            {
                // we anticipate that single-fragment messages will be common, so we optimize for them
                switch (receiveResult.MessageType)
                {
                    case WebSocketMessageType.Binary:
                        if (buffer == null)
                        {
                            message = WebSocketMessage.EmptyBinaryMessage;
                        }
                        else
                        {
                            message = new WebSocketMessage(BufferSliceToByteArray(buffer, receiveResult.Count), WebSocketMessageType.Binary);
                        }
                        break;
                    case WebSocketMessageType.Text:
                        if (buffer == null)
                        {
                            message = WebSocketMessage.EmptyTextMessage;
                        }
                        else
                        {
                            message = new WebSocketMessage(BufferSliceToString(buffer, receiveResult.Count), WebSocketMessageType.Text);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unknown message type");
                }
            }

            return message != null;
        }
    }
}
