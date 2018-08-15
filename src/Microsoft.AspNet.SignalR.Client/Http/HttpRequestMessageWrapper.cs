// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

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
                _httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
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
}

#elif NET40
// Not required on this framework.
#else 
#error Unsupported target framework.
#endif

