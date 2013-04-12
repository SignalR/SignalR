// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.IO;
using System.Net.Http;
using Microsoft.AspNet.SignalR.Client.Http;

namespace Microsoft.AspNet.SignalR.Client.WinRT.Http
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

        public Stream GetResponseStream()
        {
            return _httpResponseMessage.Content.ReadAsStreamAsync().Result;
        }

        public void Close()
        {
            _httpResponseMessage.Dispose();
        }
    }
}
