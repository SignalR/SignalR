// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NETSTANDARD1_3 || NETSTANDARD2_0

using System.IO;
using System.Net.Http;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class HttpResponseMessageWrapper : IResponse
    {
        private HttpResponseMessage _httpResponseMessage;
        
        public HttpResponseMessageWrapper(HttpResponseMessage httpResponseMessage)
        {
            _httpResponseMessage = httpResponseMessage;
        }

        public string ReadAsString()
        {
            return _httpResponseMessage.Content.ReadAsStringAsync().Result;
        }

        public Stream GetStream()
        {
            return _httpResponseMessage.Content.ReadAsStreamAsync().Result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpResponseMessage.RequestMessage.Dispose();
                _httpResponseMessage.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

#elif NET40
// Not required on this framework.
#else 
#error Unsupported target framework.
#endif
