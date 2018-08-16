// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET40

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class HttpWebResponseWrapper : IResponse
    {
        private readonly HttpWebResponse _response;

        public HttpWebResponseWrapper(HttpWebResponse response)
        {
            _response = response;
        }

        public Stream GetStream()
        {
            return _response.GetResponseStream();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)_response).Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

#elif NETSTANDARD1_3 || NET45 || NETSTANDARD2_0
// Not supported on this framework.
#else 
#error Unsupported target framework.
#endif
