using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using SignalR.Hosting.Common;

namespace SignalR.Hosting.Self
{
    public class HttpListenerRequestWrapper : IRequest
    {
        private readonly HttpListenerContext _httpListenerContext;
        private NameValueCollection _form;

        public HttpListenerRequestWrapper(HttpListenerContext httpListenerContext)
        {
            _httpListenerContext = httpListenerContext;
            QueryString = new NameValueCollection(httpListenerContext.Request.QueryString);
            Headers = new NameValueCollection(httpListenerContext.Request.Headers);
            Cookies = new CookieCollectionWrapper(httpListenerContext.Request.Cookies);
            ServerVariables = new NameValueCollection();
            User = httpListenerContext.User;

            // Set the client IP
            ServerVariables["REMOTE_ADDR"] = httpListenerContext.Request.RemoteEndPoint.Address.ToString();
        }

        public IRequestCookieCollection Cookies
        {
            get;
            private set;
        }

        public NameValueCollection Form
        {
            get
            {
                EnsureForm();
                return _form;
            }
        }

        public NameValueCollection Headers
        {
            get;
            private set;
        }

        public NameValueCollection ServerVariables
        {
            get;
            private set;
        }

        public Uri Url
        {
            get
            {
                return _httpListenerContext.Request.Url;
            }
        }

        public NameValueCollection QueryString
        {
            get;
            private set;
        }

        public IPrincipal User
        {
            get;
            private set;
        }

        private void EnsureForm()
        {
            if (_form == null)
            {
                // Do nothing if there's no body
                if (!_httpListenerContext.Request.HasEntityBody)
                {
                    _form = new NameValueCollection();
                    return;
                }

                using (var sw = new StreamReader(_httpListenerContext.Request.InputStream))
                {
                    var body = sw.ReadToEnd();
                    _form = HttpUtility.ParseDelimited(body);
                }
            }
        }

        public Task AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
#if NET45
            return _httpListenerContext.AcceptWebSocketAsync(subProtocol: null).Then(ws =>
            {
                var handler = new SignalR.WebSockets.DefaultWebSocketHandler();
                var task = handler.ProcessWebSocketRequestAsync(ws);
                callback(handler).Then(h => h.CleanClose(), handler);
                return task;
            });
#else
            throw new NotSupportedException();
#endif
        }
    }
}
