using System;
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
        private bool _connectionFailed;

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
                return !_connectionFailed && !_cancellationToken.IsCancellationRequested;
            }
        }

        public void Write(ArraySegment<byte> data)
        {
            try
            {
                _httpListenerResponse.OutputStream.Write(data.Array, data.Offset, data.Count);
            }
            catch
            {
                _connectionFailed = true;
            }
        }

        public Task FlushAsync()
        {
#if NET45
            return _httpListenerResponse.OutputStream.FlushAsync();
#else
            try
            {
                _httpListenerResponse.OutputStream.Flush();
            }
            catch
            {
                _connectionFailed = true;
            }

            return TaskAsyncHelper.Empty;
#endif
        }

        public Task EndAsync()
        {
            _httpListenerResponse.CloseSafe();
            return TaskAsyncHelper.Empty;
        }
    }
}
