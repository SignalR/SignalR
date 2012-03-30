using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting.Self.Infrastructure;

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

        public Task WriteAsync(string data)
        {
            return DoWrite(data).Then(response => response.OutputStream.Flush(), _httpListenerResponse);
        } 

        public Task EndAsync(string data)
        {
            return DoWrite(data).Then(response => response.CloseSafe(), _httpListenerResponse);
        }

        private Task DoWrite(string data)
        {
            if (!IsClientConnected)
            {
                return TaskAsyncHelper.Empty;
            }

            return _httpListenerResponse.WriteAsync(data).Catch();
        }
    }
}
