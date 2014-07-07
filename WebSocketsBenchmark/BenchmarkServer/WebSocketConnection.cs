using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BenchmarkServer
{
    public class WebSocketConnection
    {
        private static ArraySegment<byte> _readBuffer = new ArraySegment<byte>(new byte[0]);

        private readonly WebSocket _socket;
        private readonly PerformanceTracker _performanceTracker;

        public WebSocketConnection(WebSocketContext webSocketContext, PerformanceTracker performanceTracker)
        {
            _socket = webSocketContext.WebSocket;
            _performanceTracker = performanceTracker;
            _performanceTracker.ClientConnected();
        }

        public async Task Send(ArraySegment<byte> buffer)
        {
            try
            {
                await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                _performanceTracker.MessageSent();
            }
            catch (Exception) { }
        }

        public async Task Receive()
        {
            var message = await _socket.ReceiveAsync(_readBuffer, CancellationToken.None);
            if (message.CloseStatus != null)
            {
                _performanceTracker.ClientDisconnected();
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested connection close", CancellationToken.None);
            }
        }
    }
}