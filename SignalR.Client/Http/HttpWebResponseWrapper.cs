using System;
using System.IO;
using System.Net;

namespace SignalR.Client.Http
{
    public class HttpWebResponseWrapper : IResponse
    {
        private readonly IRequest _request;
        private readonly HttpWebResponse _response;

        public HttpWebResponseWrapper(IRequest request, HttpWebResponse response)
        {
            _request = request;
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
            if (_request != null)
            {
                // Always try to abort the request since close hangs if the connection is 
                // being held open
                _request.Abort();
            }

            ((IDisposable)_response).Dispose();
        }
    }
}
