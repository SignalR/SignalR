using System;
using System.Threading.Tasks;

namespace SignalR.WebSockets
{
    internal class DefaultWebSocketHandler : WebSocketHandler, IWebSocket
    {
        private bool _raiseEvent = true;

        public override void OnClose()
        {
            if (!_raiseEvent)
            {
                return;
            }

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
                onError(Error);
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

        public void CleanClose()
        {
            _raiseEvent = false;
            Close();
        }
    }
}
