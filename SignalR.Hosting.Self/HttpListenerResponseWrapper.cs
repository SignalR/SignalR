using System;
using System.Net;
using System.Threading.Tasks;
using SignalR.Hosting.Self.Infrastructure;

namespace SignalR.Hosting.Self
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
            if (!IsClientConnected)
            {
                return TaskAsyncHelper.Empty;
            }

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

        public bool Ping()
        {
            if (!IsClientConnected)
            {
                return false;
            }

            try
            {
                _httpListenerResponse.OutputStream.WriteByte(0);
                _httpListenerResponse.OutputStream.Flush();

                return true;
            }
            catch(Exception)
            {
                IsClientConnected = false;
            }

            return false;
        }

        public Task EndAsync(string data)
        {
            return WriteAsync(data).Then(response => response.CloseSafe(), _httpListenerResponse);
        }
    }
}
