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
        private readonly HttpListenerRequest _httpListenerRequest;
        private NameValueCollection _form;

        public HttpListenerRequestWrapper(HttpListenerRequest httpListenerRequest, IPrincipal user)
        {
            _httpListenerRequest = httpListenerRequest;
            QueryString = new NameValueCollection(httpListenerRequest.QueryString);
            Headers = new NameValueCollection(httpListenerRequest.Headers);
            Cookies = new CookieCollectionWrapper(_httpListenerRequest.Cookies);
            ServerVariables = new NameValueCollection();
            User = user;

            // Set the client IP
            ServerVariables["REMOTE_ADDR"] = _httpListenerRequest.RemoteEndPoint.Address.ToString();
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
                return _httpListenerRequest.Url;
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
                if (!_httpListenerRequest.HasEntityBody)
                {
                    _form = new NameValueCollection();
                    return;
                }

                using (var sw = new StreamReader(_httpListenerRequest.InputStream))
                {
                    var body = sw.ReadToEnd();
                    _form = HttpUtility.ParseDelimited(body);
                }
            }
        }

        public void AcceptWebSocketRequest(Func<IWebSocket, Task> callback)
        {
            
        }
    }
}
