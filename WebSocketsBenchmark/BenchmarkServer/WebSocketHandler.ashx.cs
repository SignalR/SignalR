using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using System.Linq;
using System.Diagnostics;

namespace BenchmarkServer
{
    /// <summary>
    /// Summary description for WebSocketHandler
    /// </summary>
    public class WebSocketHandler : IHttpHandler
    {
        private class Connection
        {
            public WebSocket Socket { get; set; }

            public async Task Send(ArraySegment<byte> buffer)
            {
                try
                {
                    await Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    Interlocked.Increment(ref _messagesTotal);
                }
                catch (Exception) { }
            }
        }

        private static ConcurrentBag<Connection> _connections = new ConcurrentBag<Connection>();

        internal static ConnectionBehavior Behavior { get; set; }

        private static Stopwatch _stopwatch = Stopwatch.StartNew();
        private static TimeSpan _lastUpdate = new TimeSpan(0);
        private static TimeSpan _lastBroadcast = new TimeSpan(0);
        private static long _messagesTotal = 0;
        private static long _lastMessagesTotal = 0;
        private static long _broadcastRate = 0;
        private static long _broadcastTime = 0;

        public static dynamic PerformanceInformation
        {
            get
            {
                var time = _stopwatch.Elapsed;
                var changeInTime = time - _lastUpdate;
                _lastUpdate = time;

                var broadcastRate = Interlocked.Read(ref _broadcastRate);
                var messagesTotal = Interlocked.Read(ref _messagesTotal);
                var changeInMessages = messagesTotal - _lastMessagesTotal;
                _lastMessagesTotal = messagesTotal;
                var broadcastTime = Interlocked.Read(ref _broadcastTime);
                

                return new
                {
                    BroadcastRate = broadcastRate,
                    MessagesTotal = messagesTotal,
                    MessagesPerSecond = (long)(1000 * changeInMessages / changeInTime.TotalMilliseconds),
                    BroadcastTime = _broadcastTime
                };
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(WebSocketLoop);
            }
            else
            {
                context.Response.ContentType = "text/plain";
                context.Response.Write("Not a WebSocket reqeust!");
                context.Response.StatusCode = 400;
            }
        }

        public static async Task Broadcast(string message)
        {
            var start = _stopwatch.Elapsed;
            var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));

            await Task.WhenAll(_connections.Select(connection => connection.Send(buffer)));

            var time = _stopwatch.Elapsed;
            var broadcastTime = (long)(time - start).TotalMilliseconds;
            Interlocked.Exchange(ref _broadcastTime, broadcastTime);

            var broadcastRate = (long)(1000 / (time - _lastBroadcast).TotalMilliseconds);
            _lastBroadcast = time;
            Interlocked.Exchange(ref _broadcastRate, broadcastRate);
        }

        private static async Task WebSocketLoop(AspNetWebSocketContext webSocketContext)
        {
            var connection = new Connection()
            {
                Socket = webSocketContext.WebSocket
            };
            _connections.Add(connection);

            var buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                var message = await connection.Socket.ReceiveAsync(buffer, CancellationToken.None);
                if (message.CloseStatus != null)
                {
                    await connection.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested connection close", CancellationToken.None);
                }
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}