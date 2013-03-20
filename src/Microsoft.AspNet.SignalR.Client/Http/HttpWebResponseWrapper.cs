// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
