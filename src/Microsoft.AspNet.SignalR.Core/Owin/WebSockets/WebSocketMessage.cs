// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD2_0

using System;
using System.Net.WebSockets;

namespace Microsoft.AspNet.SignalR.WebSockets
{
    internal sealed class WebSocketMessage
    {
        public static readonly WebSocketMessage EmptyTextMessage = new WebSocketMessage(String.Empty, WebSocketMessageType.Text);
        public static readonly WebSocketMessage EmptyBinaryMessage = new WebSocketMessage(new byte[0], WebSocketMessageType.Binary);
        public static readonly WebSocketMessage CloseMessage = new WebSocketMessage(null, WebSocketMessageType.Close);

        public readonly object Data;
        public readonly WebSocketMessageType MessageType;

        public WebSocketMessage(object data, WebSocketMessageType messageType)
        {
            Data = data;
            MessageType = messageType;
        }
    }
}

#elif NET40 || NETSTANDARD1_3
// Not supported on this framework.
#else 
#error Unsupported target framework.
#endif
