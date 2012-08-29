using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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

        public void Write(ArraySegment<byte> data)
        {
            _httpListenerResponse.OutputStream.Write(data.Array, data.Offset, data.Count);
        }

        public Task FlushAsync()
        {
#if NET45
            return _httpListenerResponse.OutputStream.FlushAsync();
#else
            _httpListenerResponse.OutputStream.Flush();
            return TaskAsyncHelper.Empty;
#endif
        }

        public Task EndAsync()
        {
            _httpListenerResponse.Close();
            return TaskAsyncHelper.Empty;
        }
    }
}
