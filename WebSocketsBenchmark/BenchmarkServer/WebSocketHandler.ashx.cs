using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using System.Linq;

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
                }
                catch (Exception) { }
            }
        }

        private static ConcurrentBag<Connection> _connections = new ConcurrentBag<Connection>();

        internal static ConnectionBehavior Behavior { get; set; }


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
            var buffer = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message));

            await Task.WhenAll(_connections.Select(connection => connection.Send(buffer)));
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