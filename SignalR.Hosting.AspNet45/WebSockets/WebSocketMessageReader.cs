using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR.WebSockets
{
    internal static class WebSocketMessageReader
    {
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

        public static async Task<WebSocketMessage> ReadMessageAsync(WebSocket webSocket, byte[] buffer, int maxMessageSize)
        {
            ArraySegment<byte> arraySegment = new ArraySegment<byte>(buffer);

            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
            // special-case close messages since they might not have the EOF flag set
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                return new WebSocketMessage(null, WebSocketMessageType.Close);
            }

            if (receiveResult.EndOfMessage)
            {
                // we anticipate that single-fragment messages will be common, so we optimize for them
                switch (receiveResult.MessageType)
                {
                    case WebSocketMessageType.Binary:
                        return new WebSocketMessage(BufferSliceToByteArray(buffer, receiveResult.Count), WebSocketMessageType.Binary);

                    case WebSocketMessageType.Text:
                        return new WebSocketMessage(BufferSliceToString(buffer, receiveResult.Count), WebSocketMessageType.Text);

                    default:
                        throw new Exception("This code path should never be hit.");
                }
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
                    receiveResult = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
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
                                throw new Exception("This code path should never be hit.");
                        }
                    }
                }
            }
        }

    }
}
