using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebSockets;

namespace SignalR.Hosting.AspNet
{
    public class AspNetWebSocketHandler : WebSocketHandler, IWebSocket
    {
        public override void OnClose()
        {
            Action onClose = ((IWebSocket)this).OnClose;
            if (onClose != null)
            {
                onClose();
            }
        }

        public override void OnError()
        {
            Action<Exception> onError = ((IWebSocket)this).OnError;
            if (onError != null)
            {
                // REVIEW: What error do we throw here?
                onError(new Exception());
            }
        }

        public override void OnMessage(string message)
        {
            Action<string> onMessage = ((IWebSocket)this).OnMessage;
            if (onMessage != null)
            {
                onMessage(message);
            }
        }

        public override void OnOpen()
        {
            base.OnOpen();
        }

        Action<string> IWebSocket.OnMessage
        {
            get;
            set;
        }

        Action IWebSocket.OnClose
        {
            get;
            set;
        }

        Action<Exception> IWebSocket.OnError
        {
            get;
            set;
        }

        Task IWebSocket.Send(string value)
        {
            Send(value);
            return TaskAsyncHelper.Empty;
        }
    }
}
