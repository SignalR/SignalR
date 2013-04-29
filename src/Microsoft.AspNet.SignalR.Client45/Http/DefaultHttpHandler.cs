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
#if !NETFX_CORE && !SILVERLIGHT && !__ANDROID__ && !IOS
    public class DefaultHttpHandler : WebRequestHandler, IRequest
#else
    public class DefaultHttpHandler : HttpClientHandler, IRequest
#endif
    {
        private readonly Action<IRequest> _prepareRequest;
        private readonly Action _cancel;
        private HttpRequestMessage _request;

        public DefaultHttpHandler(Action<IRequest> prepareRequest, Action cancel)
        {
            _prepareRequest = prepareRequest;
            _cancel = cancel;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "We call this method.")]
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _request = request;

            _prepareRequest(this);

            if (UserAgent != null)
            {
                // TODO: Fix format of user agent so that ProductInfoHeaderValue likes it
                // request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent));
            }

            if (Accept != null)
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Accept));
            }

            return base.SendAsync(request, cancellationToken);
        }

        public string UserAgent
        {
            get;
            set;
        }

        public string Accept
        {
            get;
            set;
        }

        public void SetRequestHeaders(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            foreach (KeyValuePair<string, string> headerEntry in headers)
            {
                _request.Headers.Add(headerEntry.Key, headerEntry.Value);
            }
        }

#if NET45 || NET4
        public void AddClientCerts(X509CertificateCollection certificates)
        {
            if (certificates == null)
            {
                throw new ArgumentNullException("certificates");
            }

            foreach (X509Certificate cert in certificates)
            {
                this.ClientCertificates.Add(cert);
            }
        }
#endif

        public void Abort()
        {
            _cancel();
        }
    }
}
