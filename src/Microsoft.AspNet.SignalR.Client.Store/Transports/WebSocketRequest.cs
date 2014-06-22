// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Windows.Networking.Sockets;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    internal class WebSocketRequest : IRequest
    {
        private readonly IWebSocket _webSocket;

        public WebSocketRequest(IWebSocket webSocket)
        {
            Debug.Assert(webSocket != null, "webSocket is null");

            _webSocket = webSocket;
        }

        public string UserAgent
        {
            get { return null; }
            set { }
        }

        public string Accept
        {
            get { return null; }
            set { }
        }

        public void Abort()
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This is an internal class and therefore the method is not publicly visible.")]
        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            Debug.Assert(headers != null, "headers is null");

            foreach (var header in headers)
            {
                _webSocket.SetRequestHeader(header.Key, header.Value);
            }
        }
    }
}