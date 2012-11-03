// <copyright file="Constants.cs" company="Katana contributors">
//   Copyright 2011-2012 Katana contributors
// </copyright>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Microsoft.Owin.Host.SystemWeb.WebSockets
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