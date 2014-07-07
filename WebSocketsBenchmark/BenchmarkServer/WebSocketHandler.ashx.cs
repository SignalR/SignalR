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
        private static ConcurrentBag<WebSocketConnection> _connections = new ConcurrentBag<WebSocketConnection>();
        private static PerformanceTracker _performanceTracker = new PerformanceTracker();

        internal static ConnectionBehavior Behavior { get; set; }

        public static PerformanceTracker PerformanceTracker { get { return _performanceTracker; } }

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

        public static async Task Broadcast(string message, bool reuseBuffer)
        {
            var start = DateTime.Now.Ticks;

            _performanceTracker.BroadcastStarted();
            if (reuseBuffer)
            {
                var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));
                await Task.WhenAll(_connections.Select(connection => connection.Send(buffer)));
            }
            else
            {
                await Task.WhenAll(_connections.Select(connection => connection.Send(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message)))));
            }
            _performanceTracker.BroadcastCompleted();

            var stop = DateTime.Now.Ticks;
            var time = new TimeSpan(stop - start);

            var broadcastTime = (long)time.TotalMilliseconds;
            _performanceTracker.UpdateBroadcastTime(broadcastTime);
        }

        private static async Task WebSocketLoop(AspNetWebSocketContext webSocketContext)
        {
            var connection = new WebSocketConnection(webSocketContext, PerformanceTracker);
            _connections.Add(connection);

            while (true)
            {
                await connection.Receive();
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