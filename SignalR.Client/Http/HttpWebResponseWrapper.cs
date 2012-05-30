using System;
using System.IO;
using System.Net;
using SignalR.Client.Infrastructure;

namespace SignalR.Client.Http
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
#if NET20
            return HttpHelper.ReadAsString(_response);
#else
            return _response.ReadAsString();   
#endif
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
