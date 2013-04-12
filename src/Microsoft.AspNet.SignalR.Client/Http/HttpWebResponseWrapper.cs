// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.IO;
using System.Net;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class HttpWebResponseWrapper : IResponse
    {
        private readonly HttpWebResponse _response;

        public HttpWebResponseWrapper(HttpWebResponse response)
        {
            _response = response;
        }

        public string ReadAsString()
        {
            return _response.ReadAsString();   
        }

        public Stream GetResponseStream()
        {
            return _response.GetResponseStream();
        }

        public void Close()
        {
            ((IDisposable)_response).Dispose();
        }
    }
}
