using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace BenchmarkServer
{
    /// <summary>
    /// Summary description for WebSocketHandler
    /// </summary>
    public class WebSocketHandler : IHttpHandler
    {

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

        private static async Task WebSocketLoop(AspNetWebSocketContext webSocketContext)
        {
            var socket = webSocketContext.WebSocket;

            var buffer = new ArraySegment<byte>(new byte[1024]);
            while(true)
            {
                var message = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if(message.CloseStatus != null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client requested connection close", CancellationToken.None);
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