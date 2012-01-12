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
            IsClientConnected = true;
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
            }).Catch();
        }

        public Task EndAsync(string data)
        {
            return WriteAsync(data).Then(response => response.CloseSafe(), _httpListenerResponse);
        }
    }
}
