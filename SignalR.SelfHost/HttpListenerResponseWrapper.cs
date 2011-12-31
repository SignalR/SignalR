using System.Net;
using System.Threading.Tasks;
using SignalR.Abstractions;
using SignalR.SelfHost.Infrastructure;

namespace SignalR.SelfHost
{
    public class HttpListenerResponseWrapper : IResponse
    {
        private readonly HttpListenerResponse _httpListenerResponse;

        public HttpListenerResponseWrapper(HttpListenerResponse httpListenerResponse)
        {
            _httpListenerResponse = httpListenerResponse;
            Buffer = true;
            IsClientConnected = true;
        }

        public bool Buffer
        {
            get;
            set;
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
            get;
            private set;
        }

        public Task WriteAsync(string data)
        {
            return _httpListenerResponse.WriteAsync(data).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        var ex = task.Exception.GetBaseException() as HttpListenerException;
                        if (ex != null && ex.ErrorCode == 1229)
                        {
                            // Non existent connection or connection disposed
                            IsClientConnected = false;
                        }
                    }
                    else if (!Buffer)
                    {
                        // Flush the response if we aren't buffering
                        _httpListenerResponse.OutputStream.Flush();
                    }
                });
        }
    }
}
