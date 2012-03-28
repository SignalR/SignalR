using System;
using System.Net;
using System.Threading.Tasks;
using SignalR.Hosting.Self.Infrastructure;

namespace SignalR.Hosting.Self
{
    public class HttpListenerResponseWrapper : IResponse
    {
        private readonly HttpListenerResponse _httpListenerResponse;
        private readonly object _lockObject = new object();

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
            return DoWrite(data).Then((response, lockObj) =>
            {
                lock (lockObj)
                {
                    try
                    {
                        response.OutputStream.Flush();
                    }
                    catch
                    {
                    }
                }
            }, 
            _httpListenerResponse, 
            _lockObject);
        }

        public bool Ping()
        {
            if (!IsClientConnected)
            {
                return false;
            }

            try
            {
                lock (_lockObject)
                {
                    _httpListenerResponse.OutputStream.WriteByte(0);
                    _httpListenerResponse.OutputStream.Flush();
                }

                return true;
            }
            catch (Exception)
            {
                IsClientConnected = false;
            }

            return false;
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

            lock (_lockObject)
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
        }
    }
}
