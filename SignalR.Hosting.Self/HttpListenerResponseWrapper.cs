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
        private readonly Action _onInitialWrite;
        private readonly CancellationToken _cancellationToken;

        private bool _ended;
        private int _writeInitialized;

        public HttpListenerResponseWrapper(HttpListenerResponse httpListenerResponse, Action onInitialWrite, CancellationToken cancellationToken)
        {
            _httpListenerResponse = httpListenerResponse;
            _onInitialWrite = onInitialWrite;
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
            if (Interlocked.Exchange(ref _writeInitialized, 1) == 0)
            {
                _onInitialWrite();
            }

            return DoWrite(data).Then(response => response.OutputStream.Flush(), _httpListenerResponse)
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
