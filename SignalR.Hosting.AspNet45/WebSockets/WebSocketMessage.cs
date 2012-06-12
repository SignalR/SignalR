using System;
using System.Net.WebSockets;

namespace SignalR.Hosting.AspNet.WebSockets
{
    internal sealed class WebSocketMessage
    {
        public readonly object Data;
        public readonly WebSocketMessageType MessageType;

        public WebSocketMessage(object data, WebSocketMessageType messageType)
        {
            Data = data;
            MessageType = messageType;
        }
    }
}
