// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
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

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            foreach (KeyValuePair<string, string> headerEntry in headers)
            {
                _clientWebSocket.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
            }
        }

        public void AddClientCerts(X509CertificateCollection certificates)
        {
            if (certificates == null)
            {
                throw new ArgumentNullException("certificates");
            }

            _clientWebSocket.Options.ClientCertificates = certificates;
        }

        public void Abort()
        {

        }
    }

}
