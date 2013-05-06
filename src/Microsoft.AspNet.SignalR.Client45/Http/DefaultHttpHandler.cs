// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
#if !NETFX_CORE
using System.Security.Cryptography.X509Certificates;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class HttpRequestMessageWrapper : IRequest
    {
        private readonly HttpRequestMessage _httpRequestMessage;
        private readonly Action _cancel;

        public HttpRequestMessageWrapper(HttpRequestMessage httpRequestMessage, Action cancel)
        {
            _httpRequestMessage = httpRequestMessage;
            _cancel = cancel;
        }

        public string UserAgent { get; set; }

        public string Accept { get; set; }

        public void Abort()
        {
            _cancel();
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            if (UserAgent != null)
            {
                // TODO: Fix format of user agent so that ProductInfoHeaderValue likes it
                // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
            }

            if (Accept != null)
            {
                _httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Accept));
            }

            foreach (KeyValuePair<string, string> headerEntry in headers)
            {
                _httpRequestMessage.Headers.Add(headerEntry.Key, headerEntry.Value);
            }
        }
    }


#if !NETFX_CORE && !SILVERLIGHT && !__ANDROID__ && !IOS
    public class DefaultHttpHandler : WebRequestHandler
#else
    public class DefaultHttpHandler : HttpClientHandler
#endif
    {
        private readonly IConnection _connection;

        public DefaultHttpHandler(IConnection connection)
        {
            _connection = connection;

            Credentials = _connection.Credentials;

            if (_connection.CookieContainer != null)
            {
                CookieContainer = _connection.CookieContainer;
            }

#if !SILVERLIGHT
            if (Proxy != null)
            {
                Proxy = Proxy;
            }
#endif

#if (NET4 || NET45)
            foreach (X509Certificate cert in _connection.Certificates)
            {
                ClientCertificates.Add(cert);
            }
#endif
        }
    }
}
