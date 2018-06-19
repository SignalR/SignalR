// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Web.Http.Headers;

using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class FoundationHttpRequestMessageWrapper : IRequest
    {
        private readonly Action _cancel;
        private readonly HttpRequestMessage _httpRequestMessage;

        public FoundationHttpRequestMessageWrapper(HttpRequestMessage httpRequestMessage, Action cancel)
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
                throw new ArgumentNullException(nameof(headers));
            }

            if (UserAgent != null)
            {
                _httpRequestMessage.Headers.TryAppendWithoutValidation("User-Agent", UserAgent);
            }

            if (Accept != null)
            {
                _httpRequestMessage.Headers.Accept.Add(new HttpMediaTypeWithQualityHeaderValue(Accept));
            }

            foreach (var header in headers)
            {
                _httpRequestMessage.Headers.Add(header.Key, header.Value);
            }
        }
    }
}