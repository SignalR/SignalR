using System;
using SignalR.Hosting.Self.Infrastructure;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Hosting.Self
{
    public class HttpListenerResponseWrapper : IResponse
    {
        private readonly HttpListenerResponse _httpListenerResponse;
        private readonly CancellationToken _cancellationToken;

        private bool _ended;

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
                return !_ended && !_cancellationToken.IsCancellationRequested;
            }
        }

        public Task WriteAsync(ArraySegment<byte> data)
        {
            return DoWrite(data).Then(response =>
            {
#if NET45
                return response.OutputStream.FlushAsync();
#else
                response.OutputStream.Flush();                
                return TaskAsyncHelper.Empty;
#endif

            }, _httpListenerResponse)
            .Catch(ex => _ended = true);
        }

        public Task EndAsync(ArraySegment<byte> data)
        {
            return DoWrite(data).Then(response =>
            {
                response.CloseSafe();

                // Mark the connection as ended after we close it
                _ended = true;
            }, 
            _httpListenerResponse);
        }

        private Task DoWrite(ArraySegment<byte> data)
        {
            if (!IsClientConnected)
            {
                return TaskAsyncHelper.Empty;
            }

            return _httpListenerResponse.WriteAsync(data).Catch();
        }
    }
}
