// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.Owin
{
    /// <summary>
    /// Standard keys and values for use within the OWIN interfaces
    /// </summary>
    internal static class WebSocketConstants
    {
        internal const string WebSocketSubProtocolKey = "websocket.SubProtocol";
        internal const string WebSocketSendAsyncKey = "websocket.SendAsync";
        internal const string WebSocketReceiveAyncKey = "websocket.ReceiveAsync";
        internal const string WebSocketCloseAsyncKey = "websocket.CloseAsync";
        internal const string WebSocketCallCancelledKey = "websocket.CallCancelled";
        internal const string WebSocketVersionKey = "websocket.Version";
        internal const string WebSocketVersion = "1.0";
        internal const string WebSocketCloseStatusKey = "websocket.ClientCloseStatus";
        internal const string WebSocketCloseDescriptionKey = "websocket.ClientCloseDescription";

        internal const string AspNetServerVariableWebSocketVersion = "WEBSOCKET_VERSION";
        internal const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
    }
}