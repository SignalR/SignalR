using System.Net;
using System.Threading.Tasks;
using SignalR.Hosting;
using SignalR.Hosting.Self.Infrastructure;

namespace SignalR.Hosting.Self
{
    public class HttpListenerResponseWrapper : IResponse
    {
        private readonly HttpListenerResponse _httpListenerResponse;
        private readonly CookieCollectionWrapper _cookies;

        public HttpListenerResponseWrapper(HttpListenerResponse httpListenerResponse)
        {
            _httpListenerResponse = httpListenerResponse;
            IsClientConnected = true;
            _cookies = new CookieCollectionWrapper(httpListenerResponse.Cookies,
                () => _httpListenerResponse.Cookies = new CookieCollection());
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

        public IResponseCookieCollection Cookies
        {
            get { return _cookies; }
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

        public Task EndAsync(string data)
        {
            return WriteAsync(data).Then(response => response.CloseSafe(), _httpListenerResponse);
        }
    }
}
