// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.WinRT.Http
{
    public class DefaultHttpHandler : HttpClientHandler, IRequest
    {
        private readonly Action<IRequest> _prepareRequest;
        private readonly Action _cancel;

        public DefaultHttpHandler(Action<IRequest> prepareRequest, Action cancel)
        {
            _prepareRequest = prepareRequest;
            _cancel = cancel;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "We call this method.")]
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
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

        public void Abort()
        {
            _cancel();
        }
    }
}
