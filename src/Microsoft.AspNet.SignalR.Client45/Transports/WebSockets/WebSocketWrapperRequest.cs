// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    internal class WebSocketWrapperRequest : IRequest
    {
        private readonly ClientWebSocket _clientWebSocket;

        public WebSocketWrapperRequest(ClientWebSocket clientWebSocket)
        {
            _clientWebSocket = clientWebSocket;
        }

        public string UserAgent
        {
            get
            {
                return null;
            }
            set
            {

            }
        }

        public ICredentials Credentials
        {
            get
            {
                return _clientWebSocket.Options.Credentials;
            }
            set
            {
                _clientWebSocket.Options.Credentials = value;
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                return _clientWebSocket.Options.Cookies;
            }
            set
            {
                _clientWebSocket.Options.Cookies = value;
            }
        }

        public IWebProxy Proxy
        {
            get
            {
                return _clientWebSocket.Options.Proxy;
            }
            set
            {
                _clientWebSocket.Options.Proxy = value;
            }
        }

        public string Accept
        {
            get
            {
                return null;
            }
            set
            {

            }
        }

        public void Abort()
        {

        }
    }

}
