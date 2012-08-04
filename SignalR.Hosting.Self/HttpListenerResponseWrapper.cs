using System.IO;
using SignalR.Hosting.Self.Infrastructure;
using System.Net;
using System.Threading;

namespace SignalR.Hosting.Self
{
    public class HttpListenerResponseWrapper : IResponse
    {
        private readonly HttpListenerResponse _httpListenerResponse;
        private readonly CancellationToken _cancellationToken;

        public HttpListenerResponseWrapper(HttpListenerResponse httpListenerResponse, CancellationToken cancellationToken)
        {
            _httpListenerResponse = httpListenerResponse;
            _cancellationToken = cancellationToken;
        }

        public string ContentType
        {
            get
            {
                return _httpListenerResponse.ContentType;
            }
            set
            {
                _httpListenerResponse.ContentType = value;
            }
        }

        public bool IsClientConnected
        {
            get
            {
                return !_cancellationToken.IsCancellationRequested;
            }
        }

        public Stream OutputStream
        {
            get
            {
                return _httpListenerResponse.OutputStream;
            }
        }
    }
}
